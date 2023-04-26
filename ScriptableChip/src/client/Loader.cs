using Chipz.Client;
using Chipz.Client.ComponentCode;
using GameDataAccess;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicWorld.Building.Overhaul;
using LogicWorld.ClientCode;
using LogicWorld.GameStates;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Components;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.UI;
using SChipz.Client.ComponentCode;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SChipz.Client
{
    public class ScriptableClientMod : ClientMod
    {
        private static List<ILinkable> linkables = new List<ILinkable>();
        public static void RegisterLinkable(ILinkable linkable) => linkables.Add(linkable);
        public static void UnRegisterLinkable(ILinkable linkable) => linkables.Remove(linkable);

        protected override void Initialize()
        {
            Logger.Info("Scriptable Chipz mod initialized");
        }
        #region Commands
        private static (bool success, ComponentSelection selection) CommandCheck()
        {
            if (Instances.MainWorld == null)
            {
                LConsole.WriteLine("Join a world before using this command.");
                return (false, null);
            }
            if (!MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
            {
                LConsole.WriteLine("GameStateID Invalid.");
                return (false, null);
            }

            ComponentSelection selection = ChipzClientMod.ExtractMultiSelectedObjects();
            if (selection == null)
            {
                LConsole.WriteLine("Couldn't get selection.");
                return (false, null);
            }

            return (true, selection);
        }
        [Command("sCHZ.Update", Description = "Sends an update to all Scriptable & Memory Chips telling them to reload their contents from any linked files.")]
        private static void LinkablesUpdate(bool verboseLogging = false)
        {
            int updatedCount = 0;
            int failedCount = 0;
            Stopwatch elapsedTime = new Stopwatch();
            elapsedTime.Start();
            StringBuilder toLog = new StringBuilder();
            foreach (ILinkable linkable in linkables)
            {
                if (linkable.GetLink() != string.Empty)
                {
                    string fileToLoad = Path.Combine(GameData.GameDataLocation, linkable.GetLink());
                    if (File.Exists(fileToLoad))
                    {
                        byte[] allBytes = File.ReadAllBytes(fileToLoad);
                        string allText = File.ReadAllText(fileToLoad);
                        linkable.SetContent(allBytes, allText);
                        if (linkable is ComponentClientCode ccc)
                            toLog.AppendLine($"{ccc.GetType()} @ {ccc.Address} content updated!");
                        else
                            toLog.AppendLine($"ILinkable content updated!");
                        updatedCount++;
                    }
                    else
                    {
                        if (linkable is ComponentClientCode ccc)
                            toLog.AppendLine($"{ccc.GetType()} @ {ccc.Address} contains a file link but you don't have the file! Skipping!");
                        else
                            toLog.AppendLine($"ILinkable contains a file link but you don't have the file! Skipping!");
                        failedCount++;
                    }
                }
            }
            elapsedTime.Stop();
            LConsole.WriteLine($"sCHZ.Update() Completed after {elapsedTime.Elapsed.Seconds}s.\nUpdated: {updatedCount}\nFailed: {failedCount}");
            if (verboseLogging)
                LConsole.WriteLine(toLog.ToString());
        }
        [Command("sCHZ.Link", Description = "Links the selected chip(s) with the specified file, relative to the /GameData/ folder.")]
        public static void Link(string FolderAndOrFile, bool UpdateOnCompletion = false)
        {
            (bool success, ComponentSelection selection) = CommandCheck();
            if (!success) return;

            int linkedCount = 0;
            foreach (ILinkable linkable in ChipzClientMod.RecurseSearchForChips<ILinkable>(selection))
            {
                linkable.SetLink(FolderAndOrFile);
                linkedCount++;
            }
            LConsole.WriteLine($"Successfully linked {linkedCount} chip{(linkedCount > 1 ? "s" : "")}");
            if (UpdateOnCompletion)
                LinkablesUpdate();
        }
        [Command("sCHZ.ClearLink", Description = "Clears the linked label(s)/file(s) from the selected chip(s).")]
        public static void ClearLink()
        {
            (bool success, ComponentSelection selection) = CommandCheck();
            if (!success) return;

            int clearedCount = 0;
            foreach (ILinkable linkable in ChipzClientMod.RecurseSearchForChips<ILinkable>(selection))
            {
                if (linkable.GetLink() != string.Empty)
                {
                    linkable.SetLink(string.Empty);
                    clearedCount++;
                }
            }

            LConsole.WriteLine($"Successfully cleared {clearedCount} selected chip{(clearedCount > 1 ? "s" : "")}.");
        }
        [Command("sCHZ.LinkInfo", Description = "Prints link information about the selected chip(s).")]
        public static void ChipInfo()
        {
            (bool success, ComponentSelection selection) = CommandCheck();
            if (!success) return;

            IEnumerable<ScriptableChip> SelectedChips = ChipzClientMod.RecurseSearchForChips<ScriptableChip>(selection);

            foreach (ILinkable linkable in ChipzClientMod.RecurseSearchForChips<ILinkable>(selection))
            {
                LConsole.WriteLine($"{linkable.GetType()} @ {(linkable is ComponentClientCode ccc ? ccc.Address : ComponentAddress.Null)}\n" +
                    $"Link: {(linkable.GetLink() == string.Empty ? "None" : linkable.GetLink())}\n" +
                    $"Content: {(linkable.GetContent() == string.Empty ? "None" : linkable.GetContent())}");
            }
        }
        #endregion
    }
}