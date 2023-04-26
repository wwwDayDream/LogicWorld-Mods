using LogicAPI.Server.Components;
using LogicAPI.Server.Managers;
using LogicWorld.SharedCode.BinaryStuff;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Chipz.Server.ComponentCode
{
    public abstract class DynamicChip : LogicComponent
    {
        #region Public Variables
        public IWorldMutationManager worldMutationManager => (IWorldMutationManager)((typeof(LogicComponent).GetField("WorldMutationManager", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new NullReferenceException()).GetValue(this) ?? throw new NullReferenceException());
        public IEnumerable<(int idx, bool bit)> GetInputs(int bits, int startIndex = 0)
        {
            int idx = 0;
            for (int i = startIndex; i < startIndex + bits && i < Inputs.Count; i++)
            {
                yield return (idx, Inputs[i].On);
                idx++;
            }
        }
        #endregion

    }
}
