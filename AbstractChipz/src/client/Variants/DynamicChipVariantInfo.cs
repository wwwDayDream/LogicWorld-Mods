using JimmysUnityUtilities;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using System.Collections.Generic;
using UnityEngine;

namespace Chipz.Variants
{
    public abstract class DynamicChipVariantInfo : PrefabVariantInfo
    {
        #region Internal Variables
        internal static Color24 _fakePinColor = new Color24(25, 23, 23);
        #endregion
        #region Abstract Variables
        public abstract Color24 ChipColor { get; }
        public abstract Vector2Int DefaultSize { get; }
        #endregion
        #region Virtual Variables
        public virtual Color24 FakePinColor { get { return _fakePinColor; } }
        #endregion

        #region Overrides
        public override ComponentVariant GenerateVariant(PrefabVariantIdentifier identifier)
        {
            int inputCount = identifier.InputCount;
            int outputCount = identifier.OutputCount;
            float width = inputCount / 2f;
            float height = outputCount / 2f;
            float halfWidth = inputCount / 4f;
            float halfHeight = outputCount / 4f;


            PlacingRules placingRules = new PlacingRules
            {
                OffsetDimensions = new Vector2Int((int)width, (int)height),
                DefaultOffset = new Vector2Int((int)halfWidth, (int)halfHeight),
                GridPlacingDimensions = new Vector2Int((int)width, (int)height),
                AllowFineRotation = false,
                PrimaryGridPositions = new Vector2[]
                {
                    new Vector2(0.5f, 0.5f)
                }
            };


            List<Block> prefabBlocks = new List<Block>();
            List<ComponentInput> prefabInputs = new List<ComponentInput>();
            List<ComponentOutput> prefabOutputs = new List<ComponentOutput>();

            prefabBlocks.Add(new Block()
            {
                RawColor = ChipColor,
                Scale = new Vector3(width, 1, height),
                Position = new Vector3(halfWidth - 0.5f, 0, halfHeight - 0.5f)
            });

            for (int i = 0; i < width * 2; i++)
            {
                bool firstPass = i < width;
                int iModulated = i % (int)width;
                prefabInputs.Add(new ComponentInput()
                {
                    Length = 0.6f,
                    Position = new Vector3(iModulated, 0.6f, firstPass ? -0.7f : height - 0.3f),
                    Rotation = new Vector3(180f, 0, 0)
                });
                prefabBlocks.Add(new Block()
                {
                    RawColor = FakePinColor,
                    ColliderData = new ColliderData()
                    {
                        Layer = ColliderLayer.Wire
                    },
                    Scale = new Vector3(0.5f, 0.5f, 0.4f),
                    Position = new Vector3(iModulated, 0.4f, firstPass ? -0.7f : height - 0.3f)
                });
            }

            for (int i = 0; i < height * 2; i++)
            {
                bool firstPass = i < height;
                int iModulated = i % (int)height;
                prefabOutputs.Add(new ComponentOutput()
                {
                    StartOn = false,
                    Position = new Vector3(!firstPass ? -0.5f : width - 0.5f, 0.65f, iModulated),
                    Rotation = new Vector3(0, 0, !firstPass ? 90f : -90f)
                });
                prefabBlocks.Add(new Block()
                {
                    RawColor = FakePinColor,
                    ColliderData = new ColliderData()
                    {
                        Layer = ColliderLayer.Wire
                    },
                    Scale = new Vector3(0.332f, 0.6f, 0.332f),
                    Position = new Vector3(!firstPass ? -0.7f : width - 0.3f, 0f, iModulated)
                });
            }

            return new ComponentVariant()
            {
                VariantPlacingRules = placingRules,
                VariantPrefab = new Prefab()
                {
                    Blocks = prefabBlocks.ToArray(),
                    Inputs = prefabInputs.ToArray(),
                    Outputs = prefabOutputs.ToArray()
                }
            };
        }
        public override PrefabVariantIdentifier GetDefaultComponentVariant() => new PrefabVariantIdentifier(DefaultSize.x * 2, DefaultSize.y * 2);
        #endregion
    }
}
