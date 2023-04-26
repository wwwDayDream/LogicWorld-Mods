using Chipz.Server.ComponentCode;
using JimmysUnityUtilities;
using LICC;
using LogicAPI.Server.Components;
using LogicScript.Data;
using LogicWorld.SharedCode.BinaryStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SChipz.Server.ComponentCode
{
    public static class Extensions
    {
        public static IEnumerable<bool> ToBits(this byte byt)
        {
            for (int i = 7; i >= 0; i--)
                yield return ((byt >> i) & 1) == 1;
        }
        public static IEnumerable<bool> ToBits(this byte[] byts)
        {
            foreach (byte byt in byts)
                foreach (bool b in byt.ToBits())
                    yield return b;
        }
        public static IEnumerable<byte> ToBytes(this bool[] bools)
        {
            byte curByte = 0;
            int idx = 7;
            foreach (bool b in bools)
            {
                if (b)
                    curByte += (byte)(1 << idx);
                idx--;
                if (idx < 0)
                {
                    yield return curByte;
                    curByte = 0;
                    idx = 7;
                }
            }
            if (idx != 7)
                yield return curByte;
        }
        public static IEnumerable<bool> GetBits(this byte[] bytes, int segmentIndex = 0, int segmentLength = 8)
        {
            return bytes.ToBits().Take(new Range(segmentIndex * segmentLength, (segmentIndex * segmentLength) + segmentLength));
        }
        public static IEnumerable<byte> SetBits(this byte[] bytes, bool[] bits, int segmentIndex = 0, int segmentLength = 8)
        {
            bool[] newBits = bytes.ToBits().ToArray();

            if (segmentIndex * segmentLength + segmentLength >= newBits.Length)
            {
                Array.Resize(ref newBits, segmentIndex * segmentLength + segmentLength + 1);
            }
            int idx = 0;
            for (int i = segmentIndex * segmentLength; i < newBits.Length && i < segmentIndex * segmentLength + segmentLength && idx < bits.Length; i++)
            {
                newBits[i] = bits[idx];
                idx++;
            }
            return newBits.ToBytes();
        }
    }
    public class MemoryChip : ResizableChip
    {
        #region Public Variables
        public override Vector2Int DefaultSize => new Vector2Int(4, 4);
        public string LinkedFile { get => linkedFile; set { linkedFile = value; QueueNetworkedDataUpdate(); } }
        #endregion
        #region Private Variables 
        private byte[] data = Array.Empty<byte>();
        private string linkedFile = string.Empty;
        #endregion
        protected override void DoLogicUpdate()
        {
            if (Inputs.Count < 4) return;
            int chunkCount = Outputs.Count;
            int addressCount = Math.Min((Inputs.Count - 2) - chunkCount, 32);

            int inputAddress = 0;
            GetInputs(addressCount, 0).ForEach(res =>
            {
                if (res.bit)
                    inputAddress += (1 << 7 - res.idx);
            });
            bool read = Inputs[addressCount].On;
            IEnumerable<bool> chunkIn = GetInputs(Outputs.Count, addressCount + 1).Select(f => f.bit);
            bool write = Inputs[Inputs.Count - 1].On;

            if (read)
            {
                bool[] readChunkData = data.GetBits(inputAddress, chunkCount).ToArray();
                for (int i = 0; i < Outputs.Count; i++)
                {
                    Outputs[i].On = i < readChunkData.Length ? readChunkData[i] : false;
                }
            }
            else if (write)
            {
                data = data.SetBits(chunkIn.ToArray(), inputAddress, chunkCount).ToArray();
                QueueNetworkedDataUpdate();
            }
            else
            {
                foreach (IOutputPeg output in Outputs)
                {
                    output.On = false;
                }
            }
        }

        #region Data Management
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
        protected override void OnCustomDataUpdated()
        {
        }
        #endregion

    }
}

