using JimmysUnityUtilities;
using LogicAPI.Data.BuildingRequests;
using LogicWorld.BuildingManagement;
using LogicWorld.ClientCode.Resizing;
using System;
using UnityEngine;
using LogicAPI.Data;
using Chipz.CustomData;

namespace Chipz.ComponentCode
{

    public abstract class ResizableChip<T> : DynamicChip, IResizableX, IResizableZ, IResizableCallbackReciever where T : ResizableChipCustomData, new()
    {
        #region Implement IResizableX
        public int SizeX { get => Data.SizeX; set { Data.SizeX = value; } }
        public float GridIntervalX => 1;
        public abstract int MinX { get; }
        public abstract int MaxX { get; }
        #endregion
        #region Implement IResizableZ
        public int SizeZ { get => Data.SizeZ; set { Data.SizeZ = value; } }
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
        #region Public Variables
        public T Data = new T();
        #endregion

        public ResizableChip()
        {
            DataManagementInitialize();
        }

        #region Resizing Functionality
        protected override void DataUpdate()
        {
            // This is where we need to update the size of our main block.
            SetBlockScale(0, new Vector3((float)SizeX + (Resizing ? 1 : 0), 1, (float)SizeZ + (Resizing ? 1 : 0)));
            SetBlockPosition(0, new Vector3((float)SizeX / 2 - 0.5f, 0, (float)SizeZ / 2 - 0.5f));
        }
        public void OnResizingBegin()
        {
            Resizing = true;

            ResizingColorOld = GetBlockEntity(0).Color;
            SetBlockColor(ResizingColor.ToGpuColor(), 0);
            SetBlockScale(0, new Vector3((float)SizeX + (Resizing ? 1 : 0), 1, (float)SizeZ + (Resizing ? 1 : 0)));
        }
        public void OnResizingEnd()
        {
            Resizing = false;

            SetBlockColor(ResizingColorOld, 0);

            RequestPinCountChange(SizeX * 2, SizeZ * 2);
        }
        #endregion

        #region Data Management
        internal void DataManagementInitialize()
        {
            Data.OnDataUpdateRequired += delegate ()
            {
                byte[] array = SerializeCustomData();
                QueueDataUpdate();
                if (PlacedInMainWorld)
                {
                    BuildRequestManager.SendBuildRequestWithoutAddingToUndoStack(new BuildRequest_UpdateComponentCustomData(Address, array), null);
                }
            };
        }
        protected override void DeserializeData(byte[] data)
        {
            if (data == null)
            {
                Data.SetDefaultValues();
            }
            try
            {
                Data.DeserializeData(data);
            }
            catch (OutOfMemoryException)
            {
                Data.SetDefaultValues();
            }
        }
        public override byte[] SerializeCustomData()
        {
            return Data.SerializeCustomData();
        }
        #endregion
    }
}
