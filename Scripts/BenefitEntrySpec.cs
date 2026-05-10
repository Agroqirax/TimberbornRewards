using Timberborn.BlueprintSystem;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// A single benefit entry inside BenefitSpec's array.
    /// Not a ComponentSpec itself — it's a nested value type deserialized
    /// as part of BenefitSpec, matching the pattern of GoodAmountSpec etc.
    /// </summary>
    public record BenefitEntrySpec
    {
        [Serialize]
        public string Type { get; init; } = "";

        [Serialize]
        public int Amount { get; init; }

        [Serialize]
        public int Weight { get; init; } = 1;

        [Serialize]
        public string GoodId { get; init; } = "";

        [Serialize]
        public string IconPath { get; init; } = "";
    }
}
