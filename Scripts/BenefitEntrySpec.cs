using Timberborn.BlueprintSystem;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// A single entry inside a <see cref="FactionBenefitSpec"/> benefits array.
    /// </summary>
    public record BenefitEntrySpec
    {
        /// <summary>"Science" or "Resource".</summary>
        [Serialize] public string Type   { get; init; } = "";

        /// <summary>Amount of science points or resource units to award.</summary>
        [Serialize] public int    Amount { get; init; }

        /// <summary>
        /// Relative probability weight. Defaults to 1 if omitted.
        /// An entry with Weight 3 is three times as likely to appear as one with Weight 1.
        /// </summary>
        [Serialize] public int    Weight { get; init; } = 1;

        /// <summary>GoodSpec Id for Resource benefits (e.g. "Log", "Carrot"). Ignored for Science.</summary>
        [Serialize] public string GoodId { get; init; } = "";
    }
}
