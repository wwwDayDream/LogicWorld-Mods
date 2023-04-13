using Chipz.Client.ComponentCode;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Data;
using LogicWorld.Building.Overhaul;
using LogicWorld.ClientCode;
using LogicWorld.GameStates;
using LogicWorld.Interfaces;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.UI;
using LogicWorld.UI.MainMenu;
using System;
using System.Collections.Generic;
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
        #endregion
        #region Private Variables 
        private ColoredString chipTitle = new ColoredString() { Color = Color24.White, Text = "ScriptableChip" };
        private string currentScript = string.Empty;
        private string pendingScript = string.Empty;
        private string lastSentScript = string.Empty;
        private string pendingScriptErrors = string.Empty;
        private ComponentAddress linkedLabel = ComponentAddress.Null;
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
        [Command("sCHZ.UpdateScript", Description = "Updates the selected chip(s) scripts' from the contents of the clipboard.")]
        public static void UpdateScript()
        {
            IClientWorld world = Instances.MainWorld;
            if (world == null)
            {
                LConsole.WriteLine("Join a world before using this command.");
                return;
            }
            if (MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
            {
                string clipboard = GUIUtility.systemCopyBuffer;
                ComponentSelection selection = extractMultiSelectedObjects();
                if (selection == null)
                {
                    return; //Whoops, could not get selection, stop execution.
                }

                foreach (ComponentAddress address in selection)
                {
                    IComponentClientCode code = world.Renderer.Entities.GetClientCode(address);
                    if (code != null && code is ScriptableChip)
                    {
                        ((ScriptableChip)code).PendingScipt = clipboard;
                    }
                }
            }
        }
        [Command("sCHZ.GetScript", Description = "Prints the script(s) from the selected chip(s).")]
        public static void GetScripts()
        {
            IClientWorld world = Instances.MainWorld;
            if (world == null)
            {
                LConsole.WriteLine("Join a world before using this command.");
                return;
            }
            if (MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
            {
                string clipboard = GUIUtility.systemCopyBuffer;
                ComponentSelection selection = extractMultiSelectedObjects();
                if (selection == null)
                {
                    return; //Whoops, could not get selection, stop execution.
                }

                foreach (ComponentAddress addy in selection)
                {
                    IComponentClientCode code = world.Renderer.Entities.GetClientCode(addy);
                    LConsole.WriteLine(((ScriptableChip)code).CurrentScript);
                }
            }
        }
        [Command("sCHZ.LinkLabel", Description = "Links the first selected chip with the first selected label.")]
        public static void LinkLabel()
        {
            IClientWorld world = Instances.MainWorld;
            if (world == null)
            {
                LConsole.WriteLine("Join a world before using this command.");
                return;
            }
            if (MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
            {
                ComponentSelection selection = extractMultiSelectedObjects();
                if (selection == null)
                {
                    return; //Whoops, could not get selection, stop execution.
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
        }
        [Command("sCHZ.GetLinkedLabel", Description = "Prints the linked label(s) from the selected chip(s).")]
        public static void GetLabels()
        {
            IClientWorld world = Instances.MainWorld;
            if (world == null)
            {
                LConsole.WriteLine("Join a world before using this command.");
                return;
            }
            if (MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
            {
                ComponentSelection selection = extractMultiSelectedObjects();
                if (selection == null)
                {
                    return; //Whoops, could not get selection, stop execution.
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
                    {
                        LConsole.WriteLine($"Chip @ {chip.Address} is linked to label @ {chip.LinkedLabel}");
                    }
                });
            }
        }
        [Command("sCHZ.ClearLinkedLabel", Description = "Clears the linked label(s) from the selected chip(s).")]
        public static void ClearLabel()
        {
            IClientWorld world = Instances.MainWorld;
            if (world == null)
            {
                LConsole.WriteLine("Join a world before using this command.");
                return;
            }
            if (MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
            {
                ComponentSelection selection = extractMultiSelectedObjects();
                if (selection == null)
                {
                    return; //Whoops, could not get selection, stop execution.
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
                    {
                        chip.LinkedLabel = ComponentAddress.Null;
                    }
                });
            }
        }
        [Command("sCHZ.UpdateScriptFromLabel", Description = "Updates the selected chip(s) scripts' from their linked labels.")]
        public static void UpdateLinkedLabels()
        {
            IClientWorld world = Instances.MainWorld;
            if (world == null)
            {
                LConsole.WriteLine("Join a world before using this command.");
                return;
            }
            if (MultiSelector.GameStateTextID.Equals(GameStateManager.CurrentStateID))
            {
                ComponentSelection selection = extractMultiSelectedObjects();
                if (selection == null)
                {
                    return; //Whoops, could not get selection, stop execution.
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
                    {
                        IComponentClientCode clientCode = world.Renderer.Entities.GetClientCode(chip.LinkedLabel);
                        if (clientCode is Label label)
                        {
                            LConsole.WriteLine($"Chip @ {chip.Address} pulling script from label {label.Address}");
                            chip.PendingScipt = label.Data.LabelText;
                        }
                        else
                        {
                            LConsole.WriteLine($"Chip @ {chip.Address} has a label address that doesn't point to a label! {clientCode.Address}");
                            chip.LinkedLabel = ComponentAddress.Null;
                        }
                    }
                });
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

        #region Data Management
        protected override void DataUpdate()
        {
            base.DataUpdate();
            if (pendingScriptErrors != string.Empty)
            {
                LConsole.WriteLine(pendingScriptErrors);
                PendingSciptErrors = string.Empty;
            }
        }
        public override void DeserializeNetworkedData(ref MemoryByteReader Reader)
        {
            base.DeserializeNetworkedData(ref Reader);

            currentScript = Reader.ReadString();
            pendingScript = Reader.ReadString();
            pendingScriptErrors = Reader.ReadString();
            linkedLabel = Reader.ReadComponentAddress();

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

            Writer.Write(currentScript);
            Writer.Write(pendingScript);
            Writer.Write(pendingScriptErrors);
            Writer.Write(linkedLabel);

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
            registers = Array.Empty<ulong>();
        }
        #endregion
    }
}
