using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A single entry inside a <see cref="FactionRewardSpec"/> rewards array.
    /// </summary>
    public record RewardEntrySpec
    {
        /// <summary>"Science" or "Resource".</summary>
        [Serialize] public string Type   { get; init; } = "";

        /// <summary>Amount of science points or resource units to award.</summary>
        [Serialize] public int    Amount { get; init; }

        /// <summary>
        /// Base probability weight used when no <see cref="WeightCurve"/> is
        /// present, or as the value that the curve multiplier is applied to.
        /// Defaults to 1 if omitted.
        /// An entry with Weight 3 is three times as likely to appear as one with Weight 1
        /// (before any curve scaling).
        /// </summary>
        [Serialize] public int    Weight { get; init; } = 1;

        /// <summary>GoodSpec Id for Resource rewards (e.g. "Log", "Carrot"). Ignored for Science.</summary>
        [Serialize] public string GoodId { get; init; } = "";

        /// <summary>
        /// Optional piecewise-linear curve that scales <see cref="Weight"/> by
        /// the current cycle number. When null the base weight is used every cycle.
        ///
        /// If the resolved weight is &lt;= 0 the entry is excluded from the draw
        /// entirely for that cycle, making it impossible to be offered.
        /// </summary>
        [Serialize] public WeightCurve? WeightCurve { get; init; } = null;

        /// <summary>
        /// Returns the effective integer weight at <paramref name="cycle"/>,
        /// or 0 if the entry should be excluded this cycle.
        /// </summary>
        public float GetWeightAt(int cycle)
        {
            float effectiveBase = Weight > 0 ? Weight : 1f;

            if (WeightCurve == null)
                return effectiveBase;

            return effectiveBase * WeightCurve.Evaluate(cycle);
        }
    }
}
