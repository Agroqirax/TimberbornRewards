#nullable enable
using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A single entry inside a <see cref="FactionRewardSpec"/> rewards array.
    /// </summary>
    public record RewardEntrySpec
    {
        /// <summary>"Science", "Resource", "Weather", "Need", "Building", or "Population".</summary>
        [Serialize] public string Type { get; init; } = "";

        /// <summary>
        /// The numeric value this reward operates on.
        /// Science    : science points to add (integer).
        /// Resource   : units of the good to give (integer).
        /// Weather    : days to add/remove from the season (integer; positive = longer, negative = shorter).
        /// Need       : raw points delta on the need's own scale (float; positive fills, negative drains).
        ///              e.g. Sleep has a range of [-0.2, 0.8], so 0.5 is a large fill.
        /// Building   : not used — omit or set to 0.
        /// Population : number of beavers or bots to spawn (positive integer).
        /// </summary>
        [Serialize] public float Amount { get; init; }

        /// <summary>
        /// Base probability weight. An entry with Weight 3 is three times as
        /// likely to appear as one with Weight 1 (before any curve scaling).
        /// Defaults to 1 if omitted.
        /// </summary>
        [Serialize] public int Weight { get; init; } = 1;

        /// <summary>GoodSpec Id for Resource rewards (e.g. "Log", "Carrot"). Ignored for other types.</summary>
        [Serialize] public string GoodId { get; init; } = "";

        /// <summary>
        /// For Weather rewards: "Temperate" or "Hazardous". Ignored for other types.
        /// </summary>
        [Serialize] public string Season { get; init; } = "";

        /// <summary>
        /// For Need rewards: the NeedSpec Id to modify.
        /// Vanilla beaver IDs: BadwaterContamination, Campfire, ChippedTeeth, Hunger,
        /// Injury, Lantern, Roof, RooftopTerrace, Shelter, Shrub, Sleep, Thirst, WetFur.
        /// Ignored for other types.
        /// </summary>
        [Serialize] public string NeedId { get; init; } = "";

        /// <summary>
        /// For Building rewards: the template name of the building to unlock, as it
        /// appears in <c>BuildingService.GetTemplateName</c> / <c>TemplateSpec.TemplateName</c>
        /// (e.g. "WaterWheel", "Beehive.Folktails").
        /// Ignored for other types.
        /// </summary>
        [Serialize] public string BuildingTemplateName { get; init; } = "";

        /// <summary>
        /// For Population rewards: "Beaver" or "Bot". Determines which character type
        /// is spawned. Ignored for other types.
        /// </summary>
        [Serialize] public string CharacterType { get; init; } = "";

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
