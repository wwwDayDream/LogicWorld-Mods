using JimmysUnityUtilities;
using LogicAPI.Data.BuildingRequests;
using LogicWorld.BuildingManagement;
using LogicWorld.ClientCode;
using LogicWorld.ClientCode.LabelAlignment;
using LogicWorld.Interfaces;
using LogicWorld.Interfaces.Building;
using LogicWorld.References;
using LogicWorld.Rendering.Chunks;
using LogicWorld.Rendering.Components;
using LogicWorld.SharedCode.BinaryStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using UnityEngine;
using static LogicWorld.Building.WorldOutliner;

namespace Chipz.ComponentCode
{
    public abstract class NetworkedDynamicChip : DynamicChip
    {
        #region Overrides
        public void QueueNetworkedDataUpdate()
        {
            byte[] array = SerializeCustomData();
            base.QueueDataUpdate();
            BuildRequestManager.SendBuildRequestWithoutAddingToUndoStack(new BuildRequest_UpdateComponentCustomData(Address, array), null);
        }

        #region Data Management
        public abstract void SerializeNetworkedData(ref ByteWriter Writer);
        public override byte[] SerializeCustomData()
        {
            ByteWriter byteWriter = new ByteWriter();
            SerializeNetworkedData(ref byteWriter);
            return byteWriter.Done();
        }
        public abstract void DeserializeNetworkedData(ref MemoryByteReader Reader);
        public abstract void SetDefaultNetworkedData();
        internal bool TryDeserializeData(byte[] data)
        {
            if (data == null) return false;
            bool flag = true;
            MemoryByteReader Reader = new MemoryByteReader(data);
            try
            {
                DeserializeNetworkedData(ref Reader);
            }
            catch (OutOfMemoryException)
            {
                flag = false;
            }
            Reader.Dispose();
            return flag;
        }
        protected override void DeserializeData(byte[] data)
        {
            if (!TryDeserializeData(data))
            {
                SetDefaultNetworkedData();
            }
        }
        #endregion
        #endregion
    }
}
