using LogicAPI.Server.Components;
using LogicAPI.Server.Managers;
using System.Reflection;
using Chipz.CustomData;
using System;

namespace Chipz.ComponentCode
{
    public abstract class ResizableChip<T> : LogicComponent where T : ResizableChipCustomData, new()
    {
        #region Public Variables
        public T Data = new T();
        public IWorldMutationManager worldMutationManager => (IWorldMutationManager)((typeof(LogicComponent).GetField("WorldMutationManager", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new NullReferenceException()).GetValue(this) ?? throw new NullReferenceException());
        #endregion

        public ResizableChip()
        {
            DataManagementInitialize();
        }

        #region Data Management
        internal void DataManagementInitialize()
        {
            Data.OnDataUpdateRequired += delegate ()
            {
                worldMutationManager.ForceDataRefresh(this);
            };
        }
        protected override void DeserializeData(byte[] data)
        {
            try
            {
                Data.DeserializeData(data);
            }
            catch (OutOfMemoryException)
            {
            }
        }
        protected override byte[] SerializeCustomData()
        {
            return Data.SerializeCustomData();
        }
        public override bool HasPersistentValues => true;
        #endregion
    }
}
