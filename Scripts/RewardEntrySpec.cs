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
        /// For Building rewards: the template name of the building to unlock.
        /// Ignored for other types.
        /// </summary>
        [Serialize] public string BuildingTemplateName { get; init; } = "";

        /// <summary>
        /// For Population rewards: "Beaver", "Child", or "Bot".
        /// Ignored for other types.
        /// </summary>
        [Serialize] public string CharacterType { get; init; } = "";

        // -----------------------------------------------------------------------
        // Curves — all optional. Multiple curves may be set; their multipliers
        // are multiplied together with the base weight. If none are set the base
        // weight is used unchanged every draw.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Scales weight by the current <b>cycle number</b>.
        /// Replaces the old WeightCurve field (rename in JSON: "WeightCurve" →
        /// "CycleCurve"). Existing blueprints using "WeightCurve" must be updated.
        /// </summary>
        [Serialize] public CycleCurve? CycleCurve { get; init; } = null;

        /// <summary>
        /// Scales weight by the global <b>available stock</b> of a specific good
        /// (same number shown in the top bar). Set <see cref="GoodCurve.GoodId"/>
        /// to the good to track. The x-axis keyframe values are item counts.
        /// </summary>
        [Serialize] public GoodCurve? GoodCurve { get; init; } = null;

        /// <summary>
        /// Scales weight by the global <b>average need points × 100</b> for a
        /// specific need. Set <see cref="NeedCurve.NeedId"/> to the need to track.
        /// Keyframe x-axis values are 0–100 (mapped from 0.0–1.0 need points).
        /// </summary>
        [Serialize] public NeedCurve? NeedCurve { get; init; } = null;

        /// <summary>
        /// Scales weight by the player's current <b>science point total</b>.
        /// Keyframe x-axis values are science point counts.
        /// </summary>
        [Serialize] public ScienceCurve? ScienceCurve { get; init; } = null;

        /// <summary>
        /// Scales weight by the global count of a specific <b>character type</b>.
        /// Set <see cref="PopulationCurve.CharacterType"/> to one of:
        /// "Beaver", "Child", "Bot", "Contaminated".
        /// Keyframe x-axis values are headcounts.
        /// </summary>
        [Serialize] public PopulationCurve? PopulationCurve { get; init; } = null;

        /// <summary>
        /// Returns the effective weight given the full <paramref name="context"/>.
        /// All present curves contribute a multiplier; they are all multiplied
        /// together with the base weight. Returns 0 or less to exclude the entry.
        /// </summary>
        public float GetWeightAt(in CurveContext context)
        {
            float effectiveBase = Weight > 0 ? Weight : 1f;
            float multiplier    = 1f;

            if (CycleCurve != null)
                multiplier *= CycleCurve.Evaluate(context.Cycle);

            if (GoodCurve != null)
                multiplier *= GoodCurve.Evaluate(context.Goods.Get(GoodCurve.GoodId));

            if (NeedCurve != null)
                multiplier *= NeedCurve.Evaluate(context.Needs.Get(NeedCurve.NeedId));

            if (ScienceCurve != null)
                multiplier *= ScienceCurve.Evaluate(context.SciencePoints);

            if (PopulationCurve != null)
            {
                PopulationCurveTarget? target = PopulationCurve.ParseTarget();
                if (target.HasValue)
                    multiplier *= PopulationCurve.Evaluate(context.Population.Get(target.Value));
                // Unknown CharacterType on PopulationCurve → multiplier stays 1 (no penalty).
            }

            return effectiveBase * multiplier;
        }
    }
}
