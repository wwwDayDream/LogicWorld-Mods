using LogicWorld.SharedCode.BinaryStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Chipz.CustomData
{
    public abstract class ResizableChipCustomData
    {
        #region Abstract Variables
        public static Vector2Int GetDefaultSize<T>() where T : ResizableChipCustomData, new() => new T().DefaultSize;
        public abstract Vector2Int DefaultSize { get; }
        #endregion
        #region Private Variables
        private int sizeX;
        private int sizeZ;
        #endregion
        #region Public Variables
        public int SizeX { get => sizeX; set { sizeX = value; DataUpdateRequired(); } }
        public int SizeZ { get => sizeZ; set { sizeZ = value; DataUpdateRequired(); } }
        #endregion
        #region Serialization
        public virtual byte[] DeserializeData(byte[] data)
        {
            if (data == null || data.Length < 1)
                return Array.Empty<byte>();
            byte[] finalData;
            using (MemoryByteReader memoryByteReader = new MemoryByteReader(data))
            {
                sizeX = memoryByteReader.ReadInt32();
                sizeZ = memoryByteReader.ReadInt32();
                finalData = data.Skip((int)memoryByteReader.ReadPosition).ToArray();
            }
            return finalData;
        }
        public virtual byte[] SerializeCustomData()
        {
            ByteWriter byteWriter = new ByteWriter();
            byteWriter.Write(sizeX);
            byteWriter.Write(sizeZ);
            return byteWriter.Done();
        }
        public virtual void SetDefaultValues()
        {
            sizeX = DefaultSize.x;
            sizeZ = DefaultSize.y;
        }
        #endregion
        public event Action OnDataUpdateRequired;
        public void DataUpdateRequired()
        {
            OnDataUpdateRequired?.Invoke();
        }
    }
}
