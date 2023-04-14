using Chipz.Client.ComponentCode;
using GameDataAccess;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Data;
using LogicWorld.Building.Overhaul;
using LogicWorld.ClientCode;
using LogicWorld.ClientCode.Resizing;
using LogicWorld.GameStates;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Components;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.UI;
using LogicWorld.UI.MainMenu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading;
using UnityEngine;

namespace ScriptableChip.Client.ComponentCode
{
    public class ScriptableChip : ResizableChip
    {
        #region Public Variables
        public override int MinX => 1;
        public override int MaxX => 32;
        public override int MinZ => 1;
        public override int MaxZ => 32;
        public override ColoredString ChipTitle => chipTitle;
        public override Vector2Int DefaultSize => new Vector2Int(4, 4);
        public string CurrentScript { get => currentScript; set { currentScript = value; QueueNetworkedDataUpdate(); } }
        public string PendingScipt { get => pendingScript; set { pendingScript = value; QueueNetworkedDataUpdate(); } }
        public string PendingSciptErrors { get => pendingScriptErrors; set { pendingScriptErrors = value; QueueNetworkedDataUpdate(); } }
        public ComponentAddress LinkedLabel { get => linkedLabel; set { linkedLabel = value; QueueNetworkedDataUpdate(); } }
        public string LinkedFile { get => linkedFile; set { linkedFile = value; QueueNetworkedDataUpdate(); } }
        #endregion
        #region Private Variables 
        private ColoredString chipTitle = new ColoredString() { Color = Color24.White, Text = "ScriptableChip" };
        private string currentScript = string.Empty;
        private string pendingScript = string.Empty;
        private string pendingScriptErrors = string.Empty;
        private ComponentAddress linkedLabel = ComponentAddress.Null;
        private string linkedFile = string.Empty;
        private ulong[] registers = Array.Empty<ulong>();
        #endregion

        // Thanks to Ecconia for this method
        // https://github.com/Ecconia/Ecconia-LogicWorld-Mods/blob/master/EcconiasChaosClientMod/EcconiasChaosClientMod/src/Client/ThisIsBlack.cs#L131
        private static ComponentSelection extractMultiSelectedObjects()
        {
            FieldInfo field = typeof(MultiSelector).GetField("CurrentSelection", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
            {
                LConsole.WriteLine("Could not get selection of components, as the 'CurrentSelection' field could not be found. Report this issue to the mod maintainer.");
                return null;
            }
            object value = field.GetValue(null);
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
        #region Commands
        private static IEnumerable<ScriptableChip> RecurseSearchForChips(IClientWorld world, IEnumerable<ComponentAddress> search)
        {
            if (search.Count() > 0)
            {
                IEnumerable<ScriptableChip> chipsToHit = search
                    .Select(cAddy => world.Renderer.Entities.GetClientCode(cAddy))
                    .Where(cCode => cCode is ScriptableChip)
                    .Select(cCode => cCode as ScriptableChip);

                foreach (ScriptableChip chip in chipsToHit)
                {
                    yield return chip;
                }

                IEnumerable<ScriptableChip> subChipsToHit = RecurseSearchForChips(world, search
                    .Select(cAddy => world.Renderer.Entities.GetClientCode(cAddy))
                    .Where(cCode => (cCode is Mount) || (cCode is CircuitBoard) || (cCode is ScriptableChip) || (cCode is ChipSocket))
                    .SelectMany(cCode => cCode.Component.EnumerateChildren()));
                foreach (ScriptableChip sChip in subChipsToHit)
                {
                    yield return sChip;
                }
            }
        }

        private static (bool success, string error, ComponentSelection selection) CommandCheck(IClientWorld world)
        {
            if (world == null) return (false, "Join a world before using this command.", null);

            if (!MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
                return (false, "GameStateID Invalid.", null);

            ComponentSelection selection = extractMultiSelectedObjects();
            if (selection == null)
                return (false, "Couldn't get selection.", null);

            return (true, "", selection);
        }
        [Command("sCHZ.UpdateScript", Description = "Updates the selected chip(s) scripts' from the linked file, then label, then contents of the clipboard. Whichever succeeds first.")]
        public static void UpdateScript()
        {
            IClientWorld world = Instances.MainWorld;
            string clipboard = GUIUtility.systemCopyBuffer;
            (bool success, string error, ComponentSelection selection) = CommandCheck(world);

            if (!success)
            {
                LConsole.WriteLine(error);
                return;
            }

            IEnumerable<ScriptableChip> SelectedChips = RecurseSearchForChips(world, selection);

            foreach (ScriptableChip chip in SelectedChips)
            {
                if (chip.linkedFile != string.Empty)
                {
                    string fullPath = Path.Combine(GameData.GameDataLocation, chip.linkedFile);
                    if (File.Exists(fullPath))
                    {
                        string textContent = File.ReadAllText(fullPath);
                        chip.PendingScipt = textContent;
                        LConsole.WriteLine($"Updated Chip @ {chip.Address} to script from file {fullPath}.");
                        continue;
                    }
                    else
                    {
                        LConsole.WriteLine($"Chip @ {chip.Address} has a linked file but you don't have it! Skipping! Clear the chip if you want to overwrite this.");
                        continue;
                    }
                }
                if (chip.linkedLabel != ComponentAddress.Null)
                {
                    IComponentClientCode clientCode = world.Renderer.Entities.GetClientCode(chip.linkedLabel);
                    if (clientCode is Label label)
                    {
                        string textContent = label.Data.LabelText;
                        chip.PendingScipt = textContent;
                        LConsole.WriteLine($"Updated Chip @ {chip.Address} to script from label @ {label.Address}");
                        continue;
                    }
                }
                if (GUIUtility.systemCopyBuffer.Trim() != string.Empty)
                {
                    string textContent = GUIUtility.systemCopyBuffer;
                    chip.PendingScipt = textContent;
                    LConsole.WriteLine($"Updated Chip @ {chip.Address} to script from clipboard ({textContent.Length})");
                }
            }
        }
        [Command("sCHZ.GetScript", Description = "Prints the script(s) from the selected chip(s).")]
        public static void GetScripts()
        {
            IClientWorld world = Instances.MainWorld;
            (bool success, string error, ComponentSelection selection) = CommandCheck(world);

            if (!success)
            {
                LConsole.WriteLine(error);
                return;
            }

            IEnumerable<ScriptableChip> SelectedChips = RecurseSearchForChips(world, selection);

            foreach (ScriptableChip chip in SelectedChips)
            {
                LConsole.WriteLine(chip.currentScript);
            }
        }
        [Command("sCHZ.GetData", Description = "Prints the data from the selected chip(s).")]
        public static void GetData()
        {
            IClientWorld world = Instances.MainWorld;
            (bool success, string error, ComponentSelection selection) = CommandCheck(world);

            if (!success)
            {
                LConsole.WriteLine(error);
                return;
            }

            IEnumerable<ScriptableChip> SelectedChips = RecurseSearchForChips(world, selection);

            foreach (ScriptableChip chip in SelectedChips)
            {
                LConsole.WriteLine($"Chip @ {chip.Address} Linked File: {(chip.linkedFile == string.Empty ? "None" : chip.linkedFile)} | " +
                    $"Linked Label: {(chip.linkedLabel == ComponentAddress.Null ? "None" : chip.linkedLabel.ToString())}");
            }
        }
        [Command("sCHZ.LinkLabel", Description = "Links the first selected chip with the first selected label.")]
        public static void LinkLabel()
        {
            IClientWorld world = Instances.MainWorld;

            (bool success, string error, ComponentSelection selection) = CommandCheck(world);

            if (!success)
            {
                LConsole.WriteLine(error);
                return;
            }

            IEnumerable<ScriptableChip> SelectedChips = selection.Select((addy) => world.Renderer.Entities.GetClientCode(addy))
                    .Where((code) => code is ScriptableChip)
                    .Select((code) => code as ScriptableChip);
            IEnumerable<Label> SelectedLabels = selection.Select((addy) => world.Renderer.Entities.GetClientCode(addy))
                .Where((code) => code is Label)
                .Select((code) => code as Label);

            if (SelectedChips.Count() < 1 || SelectedChips.Count() < 1)
            {
                LConsole.WriteLine("Must select 1 chip and 1 label!");
                return;
            }

            LConsole.WriteLine($"Component @ {SelectedChips.First().Address} linked to label @ {SelectedLabels.First().Address}");
            SelectedChips.First().LinkedLabel = SelectedLabels.First().Address;
        }
        [Command("sCHZ.LinkFile", Description = "Links the selected chip(s) with the specified file, relative to the /GameData/ folder.")]
        public static void LinkFile(string FolderAndOrFile)
        {
            IClientWorld world = Instances.MainWorld;

            (bool success, string error, ComponentSelection selection) = CommandCheck(world);

            if (!success)
            {
                LConsole.WriteLine(error);
                return;
            }

            IEnumerable<ScriptableChip> RecurseSearchForChips(IEnumerable<ComponentAddress> search)
            {
                if (search.Count() > 0)
                {
                    IEnumerable<ScriptableChip> chipsToHit = search
                        .Select(cAddy => world.Renderer.Entities.GetClientCode(cAddy))
                        .Where(cCode => cCode is ScriptableChip)
                        .Select(cCode => cCode as ScriptableChip);

                    foreach (ScriptableChip chip in chipsToHit)
                    {
                        yield return chip;
                    }

                    IEnumerable<ScriptableChip> subChipsToHit = RecurseSearchForChips(search
                        .Select(cAddy => world.Renderer.Entities.GetClientCode(cAddy))
                        .Where(cCode => (cCode is Mount) || (cCode is CircuitBoard) || (cCode is ScriptableChip) || (cCode is ChipSocket))
                        .SelectMany(cCode => cCode.Component.EnumerateChildren()));
                    foreach (ScriptableChip sChip in subChipsToHit)
                    {
                        yield return sChip;
                    }
                }
            }

            IEnumerable<ScriptableChip> SelectedChips = RecurseSearchForChips(selection);

            if (SelectedChips.Count() < 1)
            {
                LConsole.WriteLine("There must be at least one chip present in your selection!");
                return;
            }

            string folder = GameData.GameDataLocation;
            string finalPath = Path.Combine(folder, FolderAndOrFile);
            if (!File.Exists(finalPath))
            {
                LConsole.WriteLine($"No file found @ {finalPath}");
                return;
            }
            LConsole.WriteLine("Found file. Proceeding");

            SelectedChips.ForEach((chip) =>
            {
                chip.LinkedFile = FolderAndOrFile;
            });
        }
        [Command("sCHZ.ClearLinked", Description = "Clears the linked label(s)/file(s) from the selected chip(s).")]
        public static void Clear()
        {
            IClientWorld world = Instances.MainWorld;

            (bool success, string error, ComponentSelection selection) = CommandCheck(world);

            if (!success)
            {
                LConsole.WriteLine(error);
                return;
            }


            IEnumerable<ScriptableChip> SelectedChips = selection.Select((addy) => world.Renderer.Entities.GetClientCode(addy))
                    .Where((code) => code is ScriptableChip)
                    .Select((code) => code as ScriptableChip);

            if (SelectedChips.Count() < 1)
            {
                LConsole.WriteLine("Must select at least 1 chip!");
                return;
            }

            SelectedChips.ForEach((chip) =>
            {
                if (chip.LinkedLabel != ComponentAddress.Null)
                    chip.LinkedLabel = ComponentAddress.Null;
                if (chip.linkedFile != string.Empty)
                    chip.LinkedFile = string.Empty;
            });
        }
        [Command("sCHZ.US", Description = "Alias for sCHZ.UpdateScript")]
        public static void US() => UpdateScript();
        [Command("sCHZ.Clear", Description = "Alias for sCHZ.ClearLinked")]
        public static void CLR() => Clear();
        #endregion

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

        #region Handle Custom Label Check
        #endregion

        #region Data Management
        internal string lastParsedScript = string.Empty;
        protected override void DataUpdate()
        {
            base.DataUpdate();
            if (pendingScriptErrors != string.Empty)
            {
                LConsole.WriteLine(pendingScriptErrors);
                PendingSciptErrors = string.Empty;
            }
            if (lastParsedScript != currentScript)
            {
                lastParsedScript = currentScript;
                string title = string.Empty;
                lastParsedScript.Split(new string[] { "\r", "\n" }).Where(line => line.Replace(" ", "").StartsWith("//")).ForEach(commentLine =>
                {
                    if (commentLine.Replace(" ", "").ToLower().StartsWith("//[name]"))
                    {
                        title = commentLine.Substring(commentLine.ToLower().IndexOf("[name]") + ("[name]").Length).Trim();
                    }
                });
                if (title != string.Empty)
                {
                    chipTitle.Text = title;
                    this.QueueChipTitleUpdate();
                }
            }
        }
        public override void DeserializeNetworkedData(ref MemoryByteReader Reader)
        {
            base.DeserializeNetworkedData(ref Reader);

            currentScript = Reader.ReadString();
            pendingScript = Reader.ReadString();
            pendingScriptErrors = Reader.ReadString();
            linkedLabel = Reader.ReadComponentAddress();
            linkedFile = Reader.ReadString();

            int RegisterCount = Reader.ReadInt32();
            registers = RegisterCount == 0 ? Array.Empty<ulong>() : new ulong[RegisterCount];
            if (RegisterCount == 0) return;
            for (int i = 0; i < RegisterCount; i++)
            {
                registers[i] = Reader.ReadUInt64();
            }
        }
        public override void SerializeNetworkedData(ref ByteWriter Writer)
        {
            base.SerializeNetworkedData(ref Writer);

            Writer.Write(string.Empty);
            Writer.Write(pendingScript);
            Writer.Write(pendingScriptErrors);
            Writer.Write(linkedLabel);
            Writer.Write(linkedFile);

            Writer.Write(registers.Length);
            if (registers.Length == 0) return;
            foreach (ulong register in registers)
            {
                Writer.Write(register);
            }
        }
        public override void SetDefaultNetworkedData()
        {
            base.SetDefaultNetworkedData();
            currentScript = string.Empty;
            pendingScript = string.Empty;
            pendingScriptErrors = string.Empty;
            linkedLabel = ComponentAddress.Null;
            linkedFile = string.Empty;
            registers = Array.Empty<ulong>();
        }
        #endregion
    }
}
