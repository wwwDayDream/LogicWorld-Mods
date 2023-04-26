using Chipz.Server.ComponentCode;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Data;
using LogicAPI.Server.Components;
using LogicScript;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing;
using LogicWorld.SharedCode.BinaryStuff;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Security;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using UnityEngine;

namespace SChipz.Server.ComponentCode
{
    public class ScriptableChip : ResizableChip, IUpdatableMachine, IMachine
    {
        #region Public Variables
        public override Vector2Int DefaultSize => new Vector2Int(4, 4);
        public string CurrentScript { get => currentScript; set { currentScript = value; QueueNetworkedDataUpdate(); } }
        public string PendingScript { get => pendingScript; set { pendingScript = value; QueueNetworkedDataUpdate(); } }
        public string PendingSciptErrors { get => pendingScriptErrors; set { pendingScriptErrors = value; QueueNetworkedDataUpdate(); } }
        public string LinkedFile { get => linkedFile; set { linkedFile = value; QueueNetworkedDataUpdate(); } }
        public ulong[] Registers = Array.Empty<ulong>();
        #endregion
        #region Private Variables 
        private string currentScript = string.Empty;
        private string pendingScript = string.Empty;
        private string pendingScriptErrors = string.Empty;
        private string linkedFile = string.Empty;
        #endregion

        int IMachine.InputCount => Inputs.Count;
        int IMachine.OutputCount => Outputs.Count;

        internal bool autoUpdate = false;
        internal int updateFreq = 1;
        internal int updateCount = 1;
        internal bool hasRunStartup = false;
        internal Script compiledScript = new Script();
        internal bool IOCountLocked = false;
        protected override void DoLogicUpdate()
        {
            if (IOCountLocked)
                return;
            if (!autoUpdate || (autoUpdate && (updateCount % updateFreq == 0)))
            {
                PropertyInfo InputCountProp = typeof(Script).GetProperty("RegisteredInputLength", BindingFlags.Instance | BindingFlags.NonPublic);
                PropertyInfo OutputCountProp = typeof(Script).GetProperty("RegisteredOutputLength", BindingFlags.Instance | BindingFlags.NonPublic);
                int InputCount = int.MaxValue;
                int OutputCount = int.MaxValue;
                if (InputCountProp != null && OutputCountProp != null)
                {
                    InputCount = (int)(InputCountProp.GetValue(compiledScript) ?? 9999999);
                    OutputCount = (int)(OutputCountProp.GetValue(compiledScript) ?? 9999999);
                }
                if (Inputs.Count < InputCount)
                    LConsole.WriteLine($"Input length mismatch: Script requires {InputCount} but ScriptableChip has {Inputs.Count}");
                if (Outputs.Count < OutputCount)
                    LConsole.WriteLine($"Output length mismatch: Script requires {OutputCount} but ScriptableChip has {Outputs.Count}");
                if (Inputs.Count < InputCount || Outputs.Count < OutputCount)
                {
                    IOCountLocked = true;
                    return;
                }

                compiledScript.Run(this, !hasRunStartup, false);
                if (!hasRunStartup) hasRunStartup = true;

            }
            if (!autoUpdate) return;
            updateCount++;
            QueueLogicUpdate();
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
            if (Registers.Length != count)
            {
                Array.Resize(ref Registers, count * 64);
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
                Outputs[i].On = value[i - startIndex];
            }
        }
        void IMachine.WriteRegister(int index, BitsValue value)
        {
            Registers[index] = value.Number;
            // QueueNetworkedDataUpdate();
        }
        void IUpdatableMachine.QueueUpdate()
        {
            QueueLogicUpdate();
        }
        #endregion

        #region Data Management
        public (bool success, string error) TryCompileScript(string script)
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

                return (false, stringBuilder.ToString());
            }
            else
            {
                bool au = false;
                int uf = 1;
                script.Split(new char[] { '\r', '\n' }).Where(line => line.Replace(" ", "").StartsWith("//")).ForEach(commentLine =>
                {
                    commentLine = commentLine.Replace(" ", "").ToLower();
                    if (commentLine.StartsWith("//[autoupdate]"))
                    {
                        au = true;
                        if (commentLine.Length > ("//[autoupdate]").Length)
                        {
                            int potentionalNumber = Convert.ToInt32(commentLine.Substring(("//[autoupdate]").Length));
                            if (potentionalNumber != 0)
                            {
                                uf = potentionalNumber;
                            }
                        }
                    }
                });

                currentScript = script;
                compiledScript = result.Script;
                autoUpdate = au;
                updateFreq = uf;

                return (true, string.Empty);
            }
        }
        public override void DeserializeNetworkedData(ref MemoryByteReader Reader)
        {
            base.DeserializeNetworkedData(ref Reader);

            string suggestedCurrentScript = Reader.ReadString();
            if (suggestedCurrentScript != string.Empty && suggestedCurrentScript != currentScript)
            {
                (bool success, string error) = TryCompileScript(suggestedCurrentScript);
                if (success)
                {
                    hasRunStartup = true;

                    QueueLogicUpdate();
                }
                else
                    LConsole.WriteLine(error);
            }
            pendingScript = Reader.ReadString();
            pendingScriptErrors = Reader.ReadString();
            linkedFile = Reader.ReadString();

            int RegisterCount = Reader.ReadInt32();
            Registers = RegisterCount == 0 ? Array.Empty<ulong>() : new ulong[RegisterCount];
            for (int i = 0; i < RegisterCount; i++)
            {
                Registers[i] = Reader.ReadUInt64();
            }
        }
        public override void SerializeNetworkedData(ref ByteWriter Writer)
        {
            base.SerializeNetworkedData(ref Writer);

            Writer.Write(currentScript);
            Writer.Write(pendingScript);
            Writer.Write(pendingScriptErrors);
            Writer.Write(linkedFile);
            Writer.Write(Registers.Length);
            foreach (ulong register in Registers)
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
            linkedFile = string.Empty;
            Registers = Array.Empty<ulong>();
        }
        protected override void OnCustomDataUpdated()
        {
            if (pendingScript != string.Empty)
            {
                (bool success, string error) = TryCompileScript(pendingScript);

                if (success)
                {
                    hasRunStartup = false;

                    QueueLogicUpdate();
                }
                else
                    pendingScriptErrors = error;

                pendingScript = string.Empty;
                QueueNetworkedDataUpdate();
            }
            if (IOCountLocked)
            {
                IOCountLocked = false;
                QueueLogicUpdate();
            }
        }
        #endregion

    }
}

