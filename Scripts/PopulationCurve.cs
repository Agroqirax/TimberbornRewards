using System.Collections.Immutable;
using System.Linq;
using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// Which character type a <see cref="PopulationCurve"/> tracks.
    /// </summary>
    public enum PopulationCurveTarget
    {
        /// <summary>Total adult beavers across all districts.</summary>
        Beaver,
        /// <summary>Total beaver children across all districts.</summary>
        Child,
        /// <summary>Total bots across all districts.</summary>
        Bot,
        /// <summary>Total contaminated beavers (adults + children) across all districts.</summary>
        Contaminated,
    }

    /// <summary>
    /// A piecewise-linear curve that scales a reward's base
    /// <see cref="RewardEntrySpec.Weight"/> based on the <b>global population count</b>
    /// of a specific character type.
    ///
    /// <para>
    /// The x-axis is the total headcount across all finished districts.
    /// <see cref="CurveKeyframe.Value"/> is reused as the x-axis value (an integer count).
    /// </para>
    ///
    /// <para>
    /// Valid <see cref="CharacterType"/> values: <c>"Beaver"</c>, <c>"Child"</c>,
    /// <c>"Bot"</c>, <c>"Contaminated"</c>.
    /// </para>
    ///
    /// Example — offer more beavers when population is small:
    /// <code>
    /// "PopulationCurve": {
    ///   "CharacterType": "Beaver",
    ///   "Keyframes": [
    ///     { "Value": 0,  "Multiplier": 3.0 },
    ///     { "Value": 20, "Multiplier": 1.0 },
    ///     { "Value": 50, "Multiplier": 0.0 }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    public record PopulationCurve
    {
        /// <summary>
        /// Which character type to count. One of: "Beaver", "Child", "Bot", "Contaminated".
        /// </summary>
        [Serialize] public string CharacterType { get; init; } = "";

        [Serialize] public ImmutableArray<CurveKeyframe> Keyframes { get; init; }

        private ImmutableArray<CurveKeyframe>? _sorted;
        private ImmutableArray<CurveKeyframe> Sorted =>
            _sorted ??= Keyframes.IsDefaultOrEmpty
                ? ImmutableArray<CurveKeyframe>.Empty
                : Keyframes.OrderBy(k => k.Value).ToImmutableArray();

        /// <summary>
        /// Parses <see cref="CharacterType"/> into a <see cref="PopulationCurveTarget"/>.
        /// Returns null if the string is unrecognised.
        /// </summary>
        public PopulationCurveTarget? ParseTarget() => CharacterType switch
        {
            "Beaver"       => PopulationCurveTarget.Beaver,
            "Child"        => PopulationCurveTarget.Child,
            "Bot"          => PopulationCurveTarget.Bot,
            "Contaminated" => PopulationCurveTarget.Contaminated,
            _              => null,
        };

        /// <summary>
        /// Evaluates the multiplier for the given <paramref name="count"/> of characters.
        /// Returns 1 if no keyframes are defined or <see cref="CharacterType"/> is empty.
        /// </summary>
        public float Evaluate(float count)
        {
            ImmutableArray<CurveKeyframe> keys = Sorted;
            if (keys.IsEmpty || string.IsNullOrEmpty(CharacterType)) return 1f;
            if (count <= keys[0].Value) return keys[0].Multiplier;
            int last = keys.Length - 1;
            if (count >= keys[last].Value) return keys[last].Multiplier;
            for (int i = 0; i < last; i++)
            {
                CurveKeyframe a = keys[i];
                CurveKeyframe b = keys[i + 1];
                if (count >= a.Value && count <= b.Value)
                {
                    float t = (count - a.Value) / (b.Value - a.Value);
                    return a.Multiplier + t * (b.Multiplier - a.Multiplier);
                }
            }
            return keys[last].Multiplier;
        }
    }
}
