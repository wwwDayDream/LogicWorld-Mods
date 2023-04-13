using LogicAPI.Server.Components;
using LogicAPI.Server.Managers;
using LogicWorld.SharedCode.BinaryStuff;
using System;
using System.Reflection;
using UnityEngine;

namespace Chipz.Server.ComponentCode
{
    public abstract class ResizableChip : NetworkedDynamicChip
    {
        public abstract Vector2Int DefaultSize { get; }

        #region Private Variables
        private int sizeX;
        private int sizeZ;
        #endregion
        #region Public Variables
        public int SizeX { get => sizeX; set { sizeX = value; QueueNetworkedDataUpdate(); } }
        public int SizeZ { get => sizeZ; set { sizeZ = value; QueueNetworkedDataUpdate(); } }
        #endregion

        public override void DeserializeNetworkedData(ref MemoryByteReader Reader)
        {
            sizeX = Reader.ReadInt32();
            sizeZ = Reader.ReadInt32();
        }
        public override void SerializeNetworkedData(ref ByteWriter Writer)
        {
            Writer.Write(sizeX);
            Writer.Write(sizeZ);
        }
        public override void SetDefaultNetworkedData()
        {
            sizeX = DefaultSize.x;
            sizeZ = DefaultSize.y;
        }
    }
}
