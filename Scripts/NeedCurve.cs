using System.Collections.Immutable;
using System.Linq;
using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A piecewise-linear curve that scales a reward's base
    /// <see cref="RewardEntrySpec.Weight"/> based on the <b>global average raw need
    /// points</b> for a specific need across all living characters that have it.
    ///
    /// <para>
    /// The x-axis is the raw need points value as returned by
    /// <c>NeedManager.GetNeedPoints</c> — the same scale defined in the NeedSpec.
    /// For example, Shelter has MinimumValue=-0.2 and MaximumValue=0.8, so keyframe
    /// <see cref="CurveKeyframe.Value"/> values should be in that range.
    /// Check the relevant NeedSpec blueprint for the exact min/max of each need.
    /// </para>
    ///
    /// Example — boost thirst reward when beavers are thirsty on average:
    /// <code>
    /// "NeedCurve": {
    ///   "NeedId": "Thirst",
    ///   "Keyframes": [
    ///     { "Value": -0.2, "Multiplier": 4.0 },
    ///     { "Value":  0.4, "Multiplier": 1.0 },
    ///     { "Value":  0.8, "Multiplier": 0.0 }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    public record NeedCurve
    {
        /// <summary>The NeedSpec Id to track (e.g. "Thirst", "Hunger", "Sleep").</summary>
        [Serialize] public string NeedId { get; init; } = "";

        [Serialize] public ImmutableArray<CurveKeyframe> Keyframes { get; init; }

        private ImmutableArray<CurveKeyframe>? _sorted;
        private ImmutableArray<CurveKeyframe> Sorted =>
            _sorted ??= Keyframes.IsDefaultOrEmpty
                ? ImmutableArray<CurveKeyframe>.Empty
                : Keyframes.OrderBy(k => k.Value).ToImmutableArray();

        /// <summary>
        /// Evaluates the multiplier for the given <paramref name="averagePointsPct"/>
        /// (need points × 100, so 0–100 for a [0,1] need).
        /// Returns 1 if no keyframes are defined or <see cref="NeedId"/> is empty.
        /// </summary>
        public float Evaluate(float averagePointsPct)
        {
            ImmutableArray<CurveKeyframe> keys = Sorted;
            if (keys.IsEmpty || string.IsNullOrEmpty(NeedId)) return 1f;
            if (averagePointsPct <= keys[0].Value) return keys[0].Multiplier;
            int last = keys.Length - 1;
            if (averagePointsPct >= keys[last].Value) return keys[last].Multiplier;
            for (int i = 0; i < last; i++)
            {
                CurveKeyframe a = keys[i];
                CurveKeyframe b = keys[i + 1];
                if (averagePointsPct >= a.Value && averagePointsPct <= b.Value)
                {
                    float t = (averagePointsPct - a.Value) / (b.Value - a.Value);
                    return a.Multiplier + t * (b.Multiplier - a.Multiplier);
                }
            }
            return keys[last].Multiplier;
        }
    }
}
