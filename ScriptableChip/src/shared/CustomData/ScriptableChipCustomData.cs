using Chipz.CustomData;
using JimmysUnityUtilities;
using LogicWorld.SharedCode.BinaryStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScriptableChip.CustomData
{
    public class ScriptableChipCustomData : ResizableChipCustomData
    {
        public override Vector2Int DefaultSize => new Vector2Int(4, 4);

        #region Private Variables
        private string currentScript;
        private ulong[] registers;
        #endregion
        #region Public Variables
        public string CurrentScript { get => currentScript; set { currentScript = value; DataUpdateRequired(); } }
        public ulong[] Registers { get => registers; set { registers = value; DataUpdateRequired(); } }
        #endregion

        #region Data Management
        public override byte[] DeserializeData(byte[] data)
        {
            if (data == null || data.Length < 1)
                return Array.Empty<byte>();
            var leftOver = base.DeserializeData(data);
            byte[] final;
            using (MemoryByteReader mbr = new MemoryByteReader(leftOver))
            {
                currentScript = mbr.ReadString();
                var ulongs = mbr.ReadByteArray();
                registers = new ulong[ulongs.Length / 8];
                for (var i = 0; i < ulongs.Length / 8; i++)
                {
                    registers[i] = BitConverter.ToUInt64(mbr.ReadRaw(8), 0);
                }
                final = leftOver.Skip((int)(mbr.ReadPosition)).ToArray();
            }
            return final;
        }
        public override byte[] SerializeCustomData()
        {
            var start = base.SerializeCustomData();
            ByteWriter byteWriter = new ByteWriter();
            byteWriter.Write(currentScript);
            IEnumerable<byte> registerULongs = Array.Empty<byte>();
            registers.ForEach((data) => registerULongs = registerULongs.Concat(BitConverter.GetBytes(data)));
            byteWriter.Write(registerULongs.ToArray());     
            return start.Concat(byteWriter.Done()).ToArray();
        }
        public override void SetDefaultValues()
        {
            base.SetDefaultValues();
            currentScript = "";
            registers = Array.Empty<ulong>();
        }
        #endregion
    }
}
