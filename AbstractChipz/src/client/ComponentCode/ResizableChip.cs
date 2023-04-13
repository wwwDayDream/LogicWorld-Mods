using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicAPI.Data.BuildingRequests;
using LogicWorld.BuildingManagement;
using LogicWorld.ClientCode.Resizing;
using LogicWorld.SharedCode.BinaryStuff;
using System;
using UnityEngine;

namespace Chipz.Client.ComponentCode
{

    public abstract class ResizableChip : NetworkedDynamicChip, IResizableX, IResizableZ, IResizableCallbackReciever
    {
        public abstract Vector2Int DefaultSize { get; }

        #region Private Variables
        private int sizeX;
        private int sizeZ;
        #endregion

        #region Implement IResizableX
        public int SizeX { get => sizeX; set { sizeX = value; QueueNetworkedDataUpdate(); } }
        public float GridIntervalX => 1;
        public abstract int MinX { get; }
        public abstract int MaxX { get; }
        #endregion
        #region Implement IResizableZ
        public int SizeZ { get => sizeZ; set { sizeZ = value; QueueNetworkedDataUpdate(); } }
        public float GridIntervalZ => 1;
        public abstract int MinZ { get; }
        public abstract int MaxZ { get; }
        #endregion

        #region Internal Variables
        internal bool Resizing = false;
        internal int LastSizeX = 0;
        internal int LastSizeZ = 0;
        internal Color24 ResizingColor = Color24.CyanBlueAzure;
        internal GpuColor ResizingColorOld;
        #endregion

        #region Resizing Functionality
        protected override void DataUpdate()
        {
            // This is where we need to update the size of our main block.
            SetBlockScale(0, new Vector3(SizeX + (Resizing ? 1 : 0), 1, SizeZ + (Resizing ? 1 : 0)));
            SetBlockPosition(0, new Vector3((float)SizeX / 2 - 0.5f, 0, (float)SizeZ / 2 - 0.5f));
        }
        public void OnResizingBegin()
        {
            Resizing = true;

            ResizingColorOld = GetBlockEntity(0).Color;
            SetBlockColor(ResizingColor.ToGpuColor(), 0);
            SetBlockScale(0, new Vector3(SizeX + (Resizing ? 1 : 0), 1, SizeZ + (Resizing ? 1 : 0)));
        }
        public void OnResizingEnd()
        {
            Resizing = false;

            SetBlockColor(ResizingColorOld, 0);

            RequestPinCountChange(SizeX * 2, SizeZ * 2);
        }
        #endregion
        #region Data Management
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
        #endregion
    }
}
