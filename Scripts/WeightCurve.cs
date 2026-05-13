using System.Collections.Immutable;
using System.Linq;
using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// An optional piecewise-linear curve that scales a reward's base
    /// <see cref="RewardEntrySpec.Weight"/> based on the current cycle number.
    ///
    /// <para>
    /// Keyframes must be listed in ascending <see cref="CurveKeyframe.Cycle"/> order
    /// in the JSON. Multiplier values between keyframes are linearly interpolated.
    /// Outside the defined range the multiplier is clamped to the first or last
    /// keyframe's value.
    /// </para>
    ///
    /// <para>
    /// If the curve is absent on a <see cref="RewardEntrySpec"/> the base weight
    /// is used unchanged for every cycle.
    /// </para>
    ///
    /// Example — ramp in from cycle 10 to 20, then hold at full weight:
    /// <code>
    /// "WeightCurve": [
    ///   { "Cycle": 1,  "Multiplier": 0.0 },
    ///   { "Cycle": 10, "Multiplier": 0.0 },
    ///   { "Cycle": 20, "Multiplier": 1.0 }
    /// ]
    /// </code>
    ///
    /// Example — log that fades out by cycle 15:
    /// <code>
    /// "WeightCurve": [
    ///   { "Cycle": 1,  "Multiplier": 1.0 },
    ///   { "Cycle": 15, "Multiplier": 0.0 }
    /// ]
    /// </code>
    /// </summary>
    public record WeightCurve
    {
        // Raw keyframes as deserialized — order is not guaranteed.
        [Serialize] public ImmutableArray<CurveKeyframe> Keyframes { get; init; }

        // Sorted view, built once on first access.
        private ImmutableArray<CurveKeyframe>? _sorted;
        private ImmutableArray<CurveKeyframe> Sorted =>
            _sorted ??= Keyframes.IsDefaultOrEmpty
                ? ImmutableArray<CurveKeyframe>.Empty
                : Keyframes.OrderBy(k => k.Cycle).ToImmutableArray();

        /// <summary>
        /// Evaluates the multiplier at <paramref name="cycle"/> by linearly
        /// interpolating between the two surrounding keyframes.
        /// Keyframes may have been defined in any order in JSON — they are sorted
        /// by cycle automatically on first call.
        /// Returns 1 if the keyframe list is empty.
        /// </summary>
        public float Evaluate(int cycle)
        {
            ImmutableArray<CurveKeyframe> keys = Sorted;

            if (keys.IsEmpty)
                return 1f;

            // Before or at first keyframe — clamp to first.
            if (cycle <= keys[0].Cycle)
                return keys[0].Multiplier;

            // After or at last keyframe — clamp to last.
            int last = keys.Length - 1;
            if (cycle >= keys[last].Cycle)
                return keys[last].Multiplier;

            // Find the surrounding pair and lerp.
            for (int i = 0; i < last; i++)
            {
                CurveKeyframe a = keys[i];
                CurveKeyframe b = keys[i + 1];

                if (cycle >= a.Cycle && cycle <= b.Cycle)
                {
                    int   span = b.Cycle - a.Cycle;
                    float t    = (float)(cycle - a.Cycle) / span;
                    return a.Multiplier + t * (b.Multiplier - a.Multiplier);
                }
            }

            // Unreachable given the clamp checks above.
            return keys[last].Multiplier;
        }
    }
}