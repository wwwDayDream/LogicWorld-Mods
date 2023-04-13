using Chipz.ComponentCode;
using LICC;
using LogicAPI.Data;
using LogicScript;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicWorld.SharedCode.BinaryStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScriptableChip.Server.ComponentCode
{
    public class ScriptableChip : ResizableChip, IUpdatableMachine, IMachine
    {
        #region Public Variables
        public override Vector2Int DefaultSize => new Vector2Int(4, 4);
        public string CurrentScript { get => currentScript; set { currentScript = value; QueueNetworkedDataUpdate(); } }
        public string PendingScript { get => pendingScript; set { pendingScript = value; QueueNetworkedDataUpdate(); } }
        public string PendingSciptErrors { get => pendingScriptErrors; set { pendingScriptErrors = value; QueueNetworkedDataUpdate(); } }
        public ComponentAddress LinkedLabel { get => linkedLabel; set { linkedLabel = value; QueueNetworkedDataUpdate(); } }
        #endregion
        #region Private Variables 
        private string currentScript = string.Empty;
        private string pendingScript = string.Empty;
        private string pendingScriptErrors = string.Empty;
        private ComponentAddress linkedLabel = ComponentAddress.Null;
        private ulong[] registers = Array.Empty<ulong>();
        #endregion

        int IMachine.InputCount => Inputs.Count;
        int IMachine.OutputCount => Outputs.Count;


        internal bool hasRunStartup = false;
        internal Script compiledScript = new Script();
        protected override void DoLogicUpdate()
        {
            compiledScript.Run(this, !hasRunStartup, false);
            if (!hasRunStartup) hasRunStartup = true;
        }

        private (bool Succeeded, IReadOnlyList<Error> Errors, Script Script) TryInterpretScript(string script)
        {
            if (script == null || script == string.Empty)
                script = "";
            try
            {
                (Script Script, IReadOnlyList<Error> Errors) Result = Script.Parse(script);
                return (Result.Errors.Count == 0, Result.Errors, Result.Script);
            }
            catch
            {
                return (false, Array.Empty<Error>(), null);
            }
        }

        #region Implement IMachine
        void IMachine.AllocateRegisters(int count)
        {
            if (registers.Length != count)
            {
                Array.Resize(ref registers, count);
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
            return new BitsValue(registers[index]);
        }
        void IMachine.WriteOutput(int startIndex, Span<bool> value)
        {
            for (int i = startIndex; i < Outputs.Count && i < startIndex + value.Length; i++)
            {
                Outputs[i].On = value[i - startIndex];
            }
        }
        void IMachine.WriteRegister(int index, BitsValue value)
        {
            registers[index] = value.Number;
            QueueNetworkedDataUpdate();
        }
        void IUpdatableMachine.QueueUpdate()
        {
            QueueLogicUpdate();
        }
        #endregion

        #region Data Management
        public void TryCompileScript(string script)
        {
            (bool Succeeded, IReadOnlyList<Error> Errors, Script Script) result = TryInterpretScript(script);
            if (!result.Succeeded)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("// [ERRORS]");
                stringBuilder.AppendLine("// TryCompileScript: Found errors in the script.");
                foreach (Error error in result.Errors)
                {
                    stringBuilder.AppendLine($"// Error: {error.Severity}, Message: {error.Message}");
                }

                LConsole.WriteLine(stringBuilder.ToString());
            }
            else
            {
                compiledScript = result.Script;
                hasRunStartup = false;

                QueueLogicUpdate();
            }
        }
        public override void DeserializeNetworkedData(ref MemoryByteReader Reader)
        {
            base.DeserializeNetworkedData(ref Reader);

            currentScript = Reader.ReadString();
            TryCompileScript(currentScript);
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
        protected override void OnCustomDataUpdated()
        {
            if (pendingScript != string.Empty)
            {
                (bool Succeeded, IReadOnlyList<Error> Errors, Script Script) result = TryInterpretScript(pendingScript);
                if (!result.Succeeded)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine("// [ERRORS]");
                    stringBuilder.AppendLine("// UpdateScript: Found errors in the script.");
                    foreach (Error error in result.Errors)
                    {
                        stringBuilder.AppendLine($"// Error: {error.Severity}, Message: {error.Message}");
                    }

                    pendingScriptErrors = stringBuilder.ToString();
                }
                else
                {
                    currentScript = pendingScript;
                    compiledScript = result.Script;
                    hasRunStartup = false;

                    QueueLogicUpdate();
                }

                pendingScript = string.Empty;
                QueueNetworkedDataUpdate();
            }
        }
        #endregion

    }
}

