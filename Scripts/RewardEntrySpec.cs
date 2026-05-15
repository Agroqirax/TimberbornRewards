using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A single entry inside a <see cref="FactionRewardSpec"/> rewards array.
    /// </summary>
    public record RewardEntrySpec
    {
        /// <summary>"Science", "Resource", or "Weather".</summary>
        [Serialize] public string Type   { get; init; } = "";

        /// <summary>
        /// The numeric value this reward operates on.
        /// Science: science points to add.
        /// Resource: units of the good to give.
        /// Weather: days to add (positive) or remove (negative) from the season.
        /// </summary>
        [Serialize] public int    Amount { get; init; }

        /// <summary>
        /// Base probability weight. An entry with Weight 3 is three times as
        /// likely to appear as one with Weight 1 (before any curve scaling).
        /// Defaults to 1 if omitted.
        /// </summary>
        [Serialize] public int    Weight { get; init; } = 1;

        /// <summary>GoodSpec Id for Resource rewards (e.g. "Log", "Carrot"). Ignored for other types.</summary>
        [Serialize] public string GoodId { get; init; } = "";

        /// <summary>
        /// For Weather rewards: "Temperate" or "Hazardous".
        /// Ignored for other reward types.
        /// </summary>
        [Serialize] public string Season { get; init; } = "";

        /// <summary>
        /// Optional piecewise-linear curve that scales <see cref="Weight"/> by
        /// the current cycle number. When null the base weight is used every cycle.
        /// If the resolved weight is &lt;= 0 the entry is excluded from the draw
        /// entirely for that cycle.
        /// </summary>
        [Serialize] public WeightCurve? WeightCurve { get; init; } = null;

        /// <summary>
        /// Returns the effective weight at <paramref name="cycle"/>,
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
