using JimmysUnityUtilities;
using LICC;
using LogicWorld.Building.Overhaul;
using LogicWorld.GameStates;
using LogicWorld.Interfaces;
using LogicWorld.UI;
using System.Reflection;
using UnityEngine;
using Chipz.ComponentCode;
using ScriptableChip.CustomData;
using System;
using System.Collections.Generic;
using System.Linq;
using LogicWorld.UI.MainMenu;
using System.Runtime.InteropServices;
using LogicAPI.Data;
using LogicWorld.ClientCode;
using System.Threading;

namespace ScriptableChip.Client.ComponentCode
{
    public class ScriptableChip : ResizableChip<ScriptableChipCustomData>
    {
        #region Public Variables
        public override int MinX => 1;
        public override int MaxX => 32;
        public override int MinZ => 1;
        public override int MaxZ => 32;
        public override ColoredString ChipTitle => chipTitle;
        #endregion

        private ColoredString chipTitle = new ColoredString() { Color = Color24.White, Text = "ScriptableChip" }; 
        private static (string FirstLine, string LeftOver) RemoveFirstLine(string value)
        {
            var lines = value.Split(new string[] { "\n", "\r" }).ToList();
            var firstLine = lines[0];
            lines.RemoveAt(0);
            return (firstLine, string.Join("\n", lines));
        }
        private static (bool Success, string Script, Label Tracker) SearchForContentByLabel(string query, IComponentClientCode searchParent)
        {
            var world = Instances.MainWorld;
            IEnumerable<Label> RecursiveLabelSearch(IEnumerable<IComponentClientCode> search)
            {
                List<IComponentClientCode> BoardsToSearch = new List<IComponentClientCode>();
                foreach (var searchBase in search)
                {
                    foreach (var child in searchBase.Component.EnumerateChildren())
                    {
                        var childCode = world.Renderer.Entities.GetClientCode(child);
                        if (childCode.Component.ChildCount > 0)
                            BoardsToSearch.Add(childCode);
                        if (childCode is Label label)
                            yield return label;
                    }
                }
                RecursiveLabelSearch(BoardsToSearch);
            }

            foreach (Label label in RecursiveLabelSearch(new IComponentClientCode[] {searchParent, world.Renderer.Entities.GetClientCode(searchParent.Component.Parent) }))
            {
                var result = RemoveFirstLine(label.Data.LabelText);
                if (result.FirstLine.ToLower().StartsWith("[" + query.ToLower() + "]"))
                {
                    return (true, result.LeftOver, label);
                }
            }
            return (false, "", null);
        }
        // Thanks to Ecconia for this method
        // https://github.com/Ecconia/Ecconia-LogicWorld-Mods/blob/master/EcconiasChaosClientMod/EcconiasChaosClientMod/src/client/ThisIsBlack.cs#L131
        private static ComponentSelection extractMultiSelectedObjects()
        {
            var field = typeof(MultiSelector).GetField("CurrentSelection", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
            {
                LConsole.WriteLine("Could not get selection of components, as the 'CurrentSelection' field could not be found. Report this issue to the mod maintainer.");
                return null;
            }
            var value = field.GetValue(null);
            if (value == null)
            {
                LConsole.WriteLine("Could not get selection of components, as the current selection is 'null'. Report this issue to the mod maintainer.");
                return null;
            }
            if (!(value is ComponentSelection selection))
            {
                LConsole.WriteLine("Could not get selection of components, as the current selection is a weird type '" + value.GetType() + "'. Report this issue to the mod maintainer.");
                return null;
            }
            if (selection.Count == 0)
            {
                LConsole.WriteLine("Could not get selection of components, as nothing is selected? Report this issue to the mod maintainer.");
                return null;
            }
            return selection;
        }
        [Command("sCHZ.UpdateScript", Description = "Updates the selected chip(s) scripts to the contents of the clipboard.")]
        public static void UpdateScript()
        {
            var world = Instances.MainWorld;
            if (world == null)
            {
                LConsole.WriteLine("Join a world before using this command.");
                return;
            }
            if (MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
            {
                var clipboard = GUIUtility.systemCopyBuffer;
                var selection = extractMultiSelectedObjects();
                if (selection == null)
                {
                    return; //Whoops, could not get selection, stop execution.
                }

                foreach (var address in selection)
                {
                    IComponentClientCode code = world.Renderer.Entities.GetClientCode(address);
                    if (code != null && code is ScriptableChip)
                    {
                        ((ScriptableChip)code).SendScriptToServer(clipboard);
                    }
                }
            }
        }
        [Command("sCHZ.GetScripts", Description = "Prints the script(s) from the selected chip.")]
        public static void CopyScript()
        {
            var world = Instances.MainWorld;
            if (world == null)
            {
                LConsole.WriteLine("Join a world before using this command.");
                return;
            }
            if (MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
            {
                var clipboard = GUIUtility.systemCopyBuffer;
                var selection = extractMultiSelectedObjects();
                if (selection == null)
                {
                    return; //Whoops, could not get selection, stop execution.
                }

                foreach (var addy in selection)
                {
                    var code = world.Renderer.Entities.GetClientCode(addy);
                    LConsole.WriteLine(((ScriptableChip)code).Data.CurrentScript);
                }
            }
        }

        public override ColoredString GetInputPinLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.MiddleGreen,
                Text = i.ToString() + Superscript("IN")
            };
        }
        public override ColoredString GetOutputPinLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.MiddleBlue,
                Text = i.ToString() + Superscript("OUT")
            };
        }

        internal Label ourScript = null;
        internal string lastScriptAttempt = "";
        internal Timer updateInterval;
        protected override void Initialize()
        {
            base.Initialize();
            updateInterval = new Timer(Update, this, 0, 500);
        }
        protected static void Update(object state)
        {
            var us = (ScriptableChip)state;
            if (!us.PlacedInMainWorld)
                return;

            var Result = SearchForContentByLabel("script", us);
            if (Result.Success)
            {
                if (Result.Script != us.lastScriptAttempt)
                {
                    LConsole.WriteLine("FrameUpdate: Sending script to server...");
                    us.lastScriptAttempt = Result.Script;
                    us.SendScriptToServer(Result.Script);
                }
            }
        }

        protected override void DataUpdate()
        {
            base.DataUpdate();
            if (Data.CurrentScript == null) return;
            var result = ExtractTitleFromLogicScript(Data.CurrentScript);
            if (result.success)
                ChangeScriptTitle(result.title);
        }
        private void ChangeScriptTitle(string title)
        {
            chipTitle.Text = title;
            QueueChipTitleUpdate();
        }
        private void SendScriptToServer(string newScript)
        {
            var result = ExtractTitleFromLogicScript(newScript);
            Data.CurrentScript = newScript;
            if (result.success)
                ChangeScriptTitle(result.title);
        }
        internal (bool success, string title, string script) ExtractTitleFromLogicScript(string script)
        {
            List<string> lines = script.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (lines.Count < 1) return (false, "ScriptableChip", script);
            string firstLine = lines[0];
            string titlePrefix = "title ";

            if (lines[0].StartsWith(titlePrefix))
            {
                lines.RemoveAt(0);
                return (true, firstLine.Substring(titlePrefix.Length), string.Join("\n", lines));
            }

            return (false, "ScriptableChip", script);
        }
    }
}
