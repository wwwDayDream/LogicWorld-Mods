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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SChipz.Client.ComponentCode
{
    public class ScriptableChip : ResizableChip, ILinkable
    {
        #region Public Variables
        public override int MinX => 1;
        public override int MaxX => 32;
        public override int MinZ => 1;
        public override int MaxZ => 32;
        public override ColoredString ChipTitle => chipTitle;
        public override Vector2Int DefaultSize => new Vector2Int(4, 4);
        public string CurrentScript { get => currentScript; }
        public string PendingScipt { get => pendingScript; set { pendingScript = value; QueueNetworkedDataUpdate(); } }
        public string PendingSciptErrors { get => pendingScriptErrors; set { pendingScriptErrors = value; QueueNetworkedDataUpdate(); } }
        public string LinkedFile { get => linkedFile; set { linkedFile = value; QueueNetworkedDataUpdate(); } }
        public ulong[] Registers = Array.Empty<ulong>();
        #endregion
        #region Private Variables 
        private ColoredString chipTitle = new ColoredString() { Color = Color24.White, Text = "ScriptableChip" };
        private string currentScript = string.Empty;
        private string pendingScript = string.Empty;
        private string pendingScriptErrors = string.Empty;
        private string linkedFile = string.Empty;
        #endregion
        protected override void InitializeInWorld()
        {
            base.InitializeInWorld();
            if (!PlacedInMainWorld) return;
            ScriptableClientMod.RegisterLinkable(this);
        }
        protected override void OnComponentDestroyed()
        {
            base.OnComponentDestroyed();
            ScriptableClientMod.UnRegisterLinkable(this);
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

            Writer.Write(string.Empty);
            Writer.Write(pendingScript);
            Writer.Write(pendingScriptErrors);
            Writer.Write(linkedFile);
            Writer.Write(0);
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

        #region Implement ILinkable
        void ILinkable.SetLink(string Origin)
        {
            LinkedFile = Origin;
        }

        string ILinkable.GetLink()
        {
            return LinkedFile;
        }

        void ILinkable.SetContent(byte[] ContentBytes, string Content)
        {
            PendingScipt = Content;
        }

        string ILinkable.GetContent()
        {
            return CurrentScript;
        }
        #endregion
        #endregion
    }
}
