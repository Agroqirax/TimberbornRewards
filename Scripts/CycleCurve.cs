using System.Collections.Immutable;
using System.Linq;
using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A piecewise-linear curve that scales a reward's base
    /// <see cref="RewardEntrySpec.Weight"/> based on the current <b>cycle number</b>.
    ///
    /// <para>
    /// Keyframes must be listed in ascending <see cref="CurveKeyframe.Value"/> order
    /// in the JSON (they are sorted automatically on first evaluation if not).
    /// Multiplier values between keyframes are linearly interpolated.
    /// Outside the defined range the multiplier is clamped to the first or last
    /// keyframe's value.
    /// </para>
    ///
    /// <para>
    /// If no curve is set on a <see cref="RewardEntrySpec"/> the base weight is
    /// used unchanged for every cycle.
    /// </para>
    ///
    /// Example — fade out by cycle 15:
    /// <code>
    /// "CycleCurve": {
    ///   "Keyframes": [
    ///     { "Value": 1,  "Multiplier": 1.0 },
    ///     { "Value": 15, "Multiplier": 0.0 }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    public record CycleCurve
    {
        [Serialize] public ImmutableArray<CurveKeyframe> Keyframes { get; init; }

        private ImmutableArray<CurveKeyframe>? _sorted;
        private ImmutableArray<CurveKeyframe> Sorted =>
            _sorted ??= Keyframes.IsDefaultOrEmpty
                ? ImmutableArray<CurveKeyframe>.Empty
                : Keyframes.OrderBy(k => k.Value).ToImmutableArray();

        /// <summary>
        /// Evaluates the multiplier for the given <paramref name="cycle"/> number.
        /// Returns 1 if no keyframes are defined.
        /// </summary>
        public float Evaluate(float cycle)
        {
            ImmutableArray<CurveKeyframe> keys = Sorted;
            if (keys.IsEmpty) return 1f;
            if (cycle <= keys[0].Value) return keys[0].Multiplier;
            int last = keys.Length - 1;
            if (cycle >= keys[last].Value) return keys[last].Multiplier;
            for (int i = 0; i < last; i++)
            {
                CurveKeyframe a = keys[i];
                CurveKeyframe b = keys[i + 1];
                if (cycle >= a.Value && cycle <= b.Value)
                {
                    float t = (cycle - a.Value) / (b.Value - a.Value);
                    return a.Multiplier + t * (b.Multiplier - a.Multiplier);
                }
            }
            return keys[last].Multiplier;
        }
    }
}
