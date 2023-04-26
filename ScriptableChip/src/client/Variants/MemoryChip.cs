using Chipz.Variants;
using JimmysUnityUtilities;
using UnityEngine;

namespace SChipz.Variants
{
    public class MemoryChip : ResizableChip
    {
        public override Color24 ChipColor => Color24.Black;

        public override Vector2Int DefaultSize => new Vector2Int(4, 4);

        public override string ComponentTextID => "sCHZ.MemoryChip";
    }
}
