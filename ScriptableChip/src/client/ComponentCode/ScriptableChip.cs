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
        [Command("sCHZ.CopyScript", Description = "Copies the script from the selected chip.")]
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

                IComponentClientCode codeOfFirst = world.Renderer.Entities.GetClientCode(selection.ComponentsInSelection.ToArray()[0]);
                if (codeOfFirst != null && codeOfFirst is ScriptableChip)
                    GUIUtility.systemCopyBuffer = ((ScriptableChip)codeOfFirst).Data.CurrentScript;
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
        protected override void DataUpdate()
        {
            base.DataUpdate();
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
            LConsole.WriteLine("Uploading new script w/ length of " + newScript.Length + " to component @ address " + Address);
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
