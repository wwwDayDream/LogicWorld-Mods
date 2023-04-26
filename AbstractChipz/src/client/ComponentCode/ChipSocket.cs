
using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicWorld.Interfaces;
using LogicWorld.Interfaces.Building;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chipz.Client.ComponentCode
{
    public class ChipSocket : ResizableChip
    {
        public override Vector2Int DefaultSize => new Vector2Int(4, 4);

        public override int MinX => 1;

        public override int MaxX => 32;

        public override int MinZ => 1;

        public override int MaxZ => 32;

        public override ColoredString ChipTitle => new ColoredString()
        {
            Color = Color24.AbsoluteZero,
            Text = ""
        };
        #region Overrides
        public override ColoredString GetInputPinLabel(int i)
        {
            return new ColoredString() { Color = Color24.White, Text = "" };
        }
        public override ColoredString GetInputPinShortLabel(int i)
        {
            return new ColoredString() { Color = Color24.White, Text = "" };
        }
        public override ColoredString GetOutputPinLabel(int i)
        {
            return new ColoredString() { Color = Color24.White, Text = "" };
        }
        public override ColoredString GetOutputPinShortLabel(int i)
        {
            return new ColoredString() { Color = Color24.White, Text = "" };
        }
        protected override ChildPlacementInfo GenerateChildPlacementInfo()
        {
            List<FixedPlacingPoint> Points = new List<FixedPlacingPoint>();

            for (int i = 0; i < InputCount / 6; i++)
            {
                for (int k = 0; k < OutputCount; k++)
                {
                    Points.Add(new FixedPlacingPoint()
                    {
                        Position = new Vector3(i, 0.5f, k)
                    });
                }
            }

            // Generate placements for our size.
            return new ChildPlacementInfo()
            {
                Points = Points.ToArray()
            };
        }
        protected override void DataUpdate()
        {
            // This is where we need to update the size of our main block.
            SetBlockScale(0, new Vector3(SizeX, 0.5f, SizeZ + (Resizing ? 1 : 0)));
            SetBlockPosition(0, new Vector3((float)SizeX / 2 - 0.5f, 0, (float)SizeZ / 2 - 0.5f));

            IClientWorld world = Instances.MainWorld;
            if (world == null) return;

            if (!PlacedInMainWorld) return;

            (bool success, GpuColor color) CheckAddy(ComponentAddress Addy)
            {
                if (Addy == null || Addy == ComponentAddress.Null || !world.Renderer.Entities.ComponentIsTracked(Addy))
                    return (false, new GpuColor(0, 0, 0));
                IComponentClientCode clientCode = world.Renderer.Entities.GetClientCode(Addy);
                if (clientCode is DynamicChip dynamicChip)
                {
                    return (true, dynamicChip.GetChipColor());
                }
                return (false, new GpuColor(0, 0, 0));
            }

            (bool found, DynamicChip chip) RecurseFirstChipThatsNotSocketAsChild(ComponentAddress addy)
            {
                if (addy == null || addy == ComponentAddress.Null || !world.Renderer.Entities.ComponentIsTracked(addy))
                    return (false, null);
                IComponentClientCode clientCode = world.Renderer.Entities.GetClientCode(addy);
                if (!(clientCode is ChipSocket) && clientCode is DynamicChip dynamicChip)
                {
                    return (true, dynamicChip);
                }
                foreach (ComponentAddress address in clientCode.Component.EnumerateChildren())
                {
                    (bool found, DynamicChip chip) = RecurseFirstChipThatsNotSocketAsChild(address);
                    if (found)
                        return (true, chip);
                }
                return (false, null);
            }

            (bool found, DynamicChip chip) result = RecurseFirstChipThatsNotSocketAsChild(Address);
            if (result.found)
            {
                SetBlockColor(result.chip.GetChipColor(), 0);
                return;
            }
            (bool parentSuccess, GpuColor parentColor) = CheckAddy(Component.Parent);
            if (parentSuccess)
            {
                SetBlockColor(parentColor, 0);
                return;
            }
        }
        public override void OnResizingBegin()
        {
            Resizing = true;

            ResizingColorOld = GetBlockEntity(0).Color;
            SetBlockColor(ResizingColor.ToGpuColor(), 0);
            SetBlockScale(0, new Vector3(SizeX, 0.5f, SizeZ + (Resizing ? 1 : 0)));
        }
        public override void OnResizingEnd()
        {
            Resizing = false;

            SetBlockColor(ResizingColorOld, 0);

            RequestPinCountChange(SizeX * 6, SizeZ);
        }
        #endregion
    }
}