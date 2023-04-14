using JimmysUnityUtilities;
using LICC;
using LogicAPI.Data;
using LogicAPI.Server.Components;
using LogicAPI.Server.Managers;
using LogicAPI.WorldDataMutations;
using LogicLog;
using LogicWorld.Server;
using LogicWorld.Server.Circuitry;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace Chipz.Server.ComponentCode
{
    public class ChipSocket : ResizableChip
    {
        public override Vector2Int DefaultSize => new Vector2Int(4, 4);

        private void LinkUpPins()
        {
            List<IInputPeg> linkTo = new List<IInputPeg>();
            List<IInputPeg> linkFrom = new List<IInputPeg>();

            int state = 1;
            for (int i = 0; i < Inputs.Count; i++)
            {
                IInputPeg peg = Inputs[i];
                if (state == 1)
                {
                    linkTo.Add(peg);
                }
                else if (state == 2)
                {
                    linkFrom.Add(peg);
                }
                else if (state == 3)
                {
                    linkTo.Add(peg);
                    state = 0;
                }
                state++;
            }
            for (int i = 0; i < linkFrom.Count; i++)
            {
                linkFrom[i].RemoveAllSecretLinks();
                linkFrom[i].AddSecretLinkWith(linkTo[i]);
                linkFrom[i].AddSecretLinkWith(linkTo[linkFrom.Count + i]);
            }
        }

        internal int oldSizeX = 0;
        internal int oldSizeZ = 0;
        protected override void OnCustomDataUpdated()
        {
            base.OnCustomDataUpdated();
            if (this.SizeX != oldSizeX || this.SizeZ != oldSizeZ)
            {
                LinkUpPins();
            }
        }
        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex)
        {
            return false;
        }
    }
}