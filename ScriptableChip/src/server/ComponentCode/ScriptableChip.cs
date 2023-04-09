using Chipz.ComponentCode;
using LICC;
using LogicScript;
using LogicScript.Data;
using LogicScript.Parsing;
using ScriptableChip.CustomData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptableChip.Server.ComponentCode
{
    public class ScriptableChip : ResizableChip<ScriptableChipCustomData>, IUpdatableMachine, IMachine
    {
        int IMachine.InputCount => Inputs.Count;

        int IMachine.OutputCount => Outputs.Count;

        private ulong[] Registers = Array.Empty<ulong>();

        internal bool hasRunStartup = false;
        internal string oldScript = "";
        internal Script CompiledScript = new Script();
        protected override void DoLogicUpdate()
        {
            CompiledScript.Run(this, !hasRunStartup, false);
            if (!hasRunStartup) hasRunStartup = true;
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
        private void UpdateScript()
        {
            if (oldScript == Data.CurrentScript)
            {
                LConsole.WriteLine("UpdateScript: oldScript is the same as Data.CurrentScript - returning early.");
                return;
            }

            LConsole.WriteLine("UpdateScript: Parsing script.");
            var removedTitle = ExtractTitleFromLogicScript(Data.CurrentScript);

            var result = Script.Parse(removedTitle.script);

            if (result.Errors.Count > 0)
            {
                LConsole.WriteLine("UpdateScript: Found errors in the script - returning early.");
                foreach (var error in result.Errors)
                {
                    LConsole.WriteLine($"Error: {error.Severity}, Message: {error.Message}");
                }
                Data.CurrentScript = oldScript;
                return;
            }

            if (result.Script == null)
            {
                LConsole.WriteLine("UpdateScript: The parsed script is null - throwing exception.");
                Data.CurrentScript = oldScript;
            }
            else
            {
                LConsole.WriteLine("UpdateScript: Assigning CompiledScript.");
                CompiledScript = result.Script;
            }

            LConsole.WriteLine("UpdateScript: Updating oldScript.");
            oldScript = Data.CurrentScript;

            LConsole.WriteLine("UpdateScript: Setting hasRunStartup to false.");
            hasRunStartup = false;

            LConsole.WriteLine("UpdateScript: Queuing logic update.");
            QueueLogicUpdate();
        }

        void IMachine.AllocateRegisters(int count)
        {
            if (Registers.Length != count)
            {
                Array.Resize(ref Registers, count);
            }
        }

        void IMachine.Print(string msg)
        {
            LConsole.BeginLine()
                .Write("[ScriptableChip]", CColor.Blue)
                .Write(" (" + Address + ") ", CColor.Green)
                .Write(msg, CColor.Yellow).End();
        }

        void IMachine.ReadInput(Span<bool> values)
        {
            for (int i = 0; i < Inputs.Count; i++)
            {
                values[i] = Inputs[i].On;
            }
        }

        BitsValue IMachine.ReadRegister(int index)
        {
            return new BitsValue(Registers[index]);
        }

        void IMachine.WriteOutput(int startIndex, Span<bool> value)
        {
            for (int i = startIndex; i < Outputs.Count && i < startIndex + value.Length; i++)
            {
                Outputs[i].On = value[i];
            }
        }

        void IMachine.WriteRegister(int index, BitsValue value)
        {
            Registers[index] = value.Number;
        }

        void IUpdatableMachine.QueueUpdate()
        {
            QueueLogicUpdate();
        }

        protected override void OnCustomDataUpdated()
        {
            if (Data.CurrentScript == null) return;
            Logger.Info("Custom Script Received: " + Data.CurrentScript);
            UpdateScript();
        }
    }
}

