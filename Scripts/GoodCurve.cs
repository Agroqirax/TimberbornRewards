using System.Collections.Immutable;
using System.Linq;
using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A piecewise-linear curve that scales a reward's base
    /// <see cref="RewardEntrySpec.Weight"/> based on the <b>global available stock</b>
    /// of a specific good (the same number shown in the top bar).
    ///
    /// <para>
    /// The x-axis is the good amount (<see cref="CurveKeyframe.Value"/> is reused
    /// as the amount axis). The evaluated multiplier is linearly interpolated between
    /// keyframes exactly as in <see cref="CycleCurve"/>.
    /// </para>
    ///
    /// <para>
    /// Set <see cref="GoodId"/> to the good you want to track (e.g. "Log", "Plank").
    /// If the good is not found the curve returns 1 (no effect).
    /// </para>
    ///
    /// Example — offer more logs when stockpile is low:
    /// <code>
    /// "GoodCurve": {
    ///   "GoodId": "Log",
    ///   "Keyframes": [
    ///     { "Value": 0,   "Multiplier": 2.0 },
    ///     { "Value": 100, "Multiplier": 0.0 }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    public record GoodCurve
    {
        /// <summary>The GoodSpec Id to track (e.g. "Log", "Plank", "Water").</summary>
        [Serialize] public string GoodId { get; init; } = "";

        [Serialize] public ImmutableArray<CurveKeyframe> Keyframes { get; init; }

        private ImmutableArray<CurveKeyframe>? _sorted;
        private ImmutableArray<CurveKeyframe> Sorted =>
            _sorted ??= Keyframes.IsDefaultOrEmpty
                ? ImmutableArray<CurveKeyframe>.Empty
                : Keyframes.OrderBy(k => k.Value).ToImmutableArray();

        /// <summary>
        /// Evaluates the multiplier for the given <paramref name="amount"/> of the good.
        /// Returns 1 if no keyframes are defined or <see cref="GoodId"/> is empty.
        /// </summary>
        public float Evaluate(float amount)
        {
            ImmutableArray<CurveKeyframe> keys = Sorted;
            if (keys.IsEmpty || string.IsNullOrEmpty(GoodId)) return 1f;
            if (amount <= keys[0].Value) return keys[0].Multiplier;
            int last = keys.Length - 1;
            if (amount >= keys[last].Value) return keys[last].Multiplier;
            for (int i = 0; i < last; i++)
            {
                CurveKeyframe a = keys[i];
                CurveKeyframe b = keys[i + 1];
                if (amount >= a.Value && amount <= b.Value)
                {
                    float t = (amount - a.Value) / (b.Value - a.Value);
                    return a.Multiplier + t * (b.Multiplier - a.Multiplier);
                }
            }
            return keys[last].Multiplier;
        }
    }
}
