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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SChipz.Client.ComponentCode
{
    public class MemoryChip : ResizableChip, ILinkable
    {
        #region Public Variables
        public override int MinX => 1;
        public override int MaxX => 32;
        public override int MinZ => 1;
        public override int MaxZ => 32;
        public override Vector2Int DefaultSize => new Vector2Int(4, 4);
        public override ColoredString ChipTitle => chipTitle;
        public byte[] Data { get => data; set { data = value; QueueNetworkedDataUpdate(); } }
        public string LinkedFile { get => linkedFile; set { linkedFile = value; QueueNetworkedDataUpdate(); } }
        #endregion
        #region Private Variables 
        private ColoredString chipTitle = new ColoredString() { Color = Color24.White, Text = "RAM Chip" };
        private byte[] data = Array.Empty<byte>();
        private string linkedFile = string.Empty;
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
            Data = ContentBytes;
        }

        string ILinkable.GetContent()
        {
            StringBuilder sb = new StringBuilder("\n");
            int idx = 1;
            foreach (byte byt in Data)
            {
                sb.Append(Convert.ToString(byt, 2).PadLeft(8, '0') + " ");
                if (idx == 4)
                {
                    sb.Append("\n");
                    idx = 1;
                }
                else
                    idx++;
            }
            return sb.ToString();
        }
        #endregion

        #region Data Management
        protected override void DataUpdate()
        {
            base.DataUpdate();
        }
        public override void DeserializeNetworkedData(ref MemoryByteReader Reader)
        {
            base.DeserializeNetworkedData(ref Reader);

            data = Reader.ReadByteArray();
            linkedFile = Reader.ReadString();
        }
        public override void SerializeNetworkedData(ref ByteWriter Writer)
        {
            base.SerializeNetworkedData(ref Writer);

            Writer.Write(data);
            Writer.Write(linkedFile);
        }
        public override void SetDefaultNetworkedData()
        {
            base.SetDefaultNetworkedData();

            data = Array.Empty<byte>();
            linkedFile = string.Empty;
        }
        #endregion
    }
}
