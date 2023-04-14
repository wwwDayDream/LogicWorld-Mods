
using JimmysUnityUtilities;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using System.Collections.Generic;
using UnityEngine;

namespace Chipz.Variants
{
    public class ChipSocketVariantInfo : DynamicChipVariantInfo
    {
        public override Color24 ChipColor => Color24.LightGray;

        public override Vector2Int DefaultSize => new Vector2Int(4, 4);

        public override string ComponentTextID => "CHZ.ChipSocket";
        #region Overrides
        public override ComponentVariant GenerateVariant(PrefabVariantIdentifier identifier)
        {
            int inputCount = identifier.InputCount;
            int outputCount = identifier.OutputCount;
            float width = inputCount / 6f;
            float height = outputCount;
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;


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
                Scale = new Vector3(width, 0.5f, height),
                Position = new Vector3(halfWidth - 0.5f, 0, halfHeight - 0.5f)
            });

            for (int i = 0; i < width * 2; i++)
            {
                bool firstPass = i < width;
                int iModulated = i % (int)width;
                prefabInputs.Add(new ComponentInput()
                {
                    Length = 0.48f,
                    Position = new Vector3(iModulated - 0.333f, 0.01f, firstPass ? -0.5f : height - 0.5f),
                    Rotation = new Vector3(0f, 0, 0),
                    Bottomless = false,
                });
                prefabInputs.Add(new ComponentInput()
                {
                    Length = 0.33f,
                    Position = new Vector3(iModulated, 0.05f, firstPass ? -0.7f : height - 0.3f),
                    Rotation = new Vector3(0f, 0, 0),
                    Bottomless = false,
                });
                prefabInputs.Add(new ComponentInput()
                {
                    Length = 0.48f,
                    Position = new Vector3(iModulated + 0.333f, 0.01f, firstPass ? -0.5f : height - 0.5f),
                    Rotation = new Vector3(0f, 0, 0),
                    Bottomless = false,
                });
            }

            for (int i = 0; i < height; i++)
            {
                prefabOutputs.Add(new ComponentOutput()
                {
                    Position = new Vector3(halfWidth, 0, 0),
                    StartOn = false,
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
        public override PrefabVariantIdentifier GetDefaultComponentVariant() => new PrefabVariantIdentifier(DefaultSize.x * 6, DefaultSize.y);
        #endregion
    }
}
