using LogicAPI.Server.Components;
using LogicAPI.Server.Managers;
using LogicWorld.SharedCode.BinaryStuff;
using System;
using System.Reflection;

namespace Chipz.Server.ComponentCode
{
    public abstract class NetworkedDynamicChip : LogicComponent
    {
        #region Public Variables
        public IWorldMutationManager worldMutationManager => (IWorldMutationManager)((typeof(LogicComponent).GetField("WorldMutationManager", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new NullReferenceException()).GetValue(this) ?? throw new NullReferenceException());
        #endregion

        #region Overrides
        public void QueueNetworkedDataUpdate()
        {
            worldMutationManager.ForceDataRefresh(this);
        }
        #region Data Management
        public abstract void SerializeNetworkedData(ref ByteWriter Writer);
        protected override byte[] SerializeCustomData()
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
                QueueNetworkedDataUpdate();
            }
        }
        public override bool HasPersistentValues => true;
        #endregion
        #endregion
    }
}
