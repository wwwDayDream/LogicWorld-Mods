using Chipz.CustomData;
using Chipz.Variants;
using JimmysUnityUtilities;
using ScriptableChip.CustomData;
using UnityEngine;

namespace ScriptableChip.Variants
{
    public class ScriptableChipVariantInfo : ResizableChipVariantInfo
    {
        public override Color24 ChipColor => Color24.Black;

        public override Vector2Int DefaultSize
        {
            get
            {
                return ResizableChipCustomData.GetDefaultSize<ScriptableChipCustomData>();
            }
        }

        public override string ComponentTextID => "sCHZ.ScriptableChip";
    }
}
