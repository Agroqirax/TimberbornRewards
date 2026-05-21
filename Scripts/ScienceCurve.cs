using System.Collections.Immutable;
using System.Linq;
using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A piecewise-linear curve that scales a reward's base
    /// <see cref="RewardEntrySpec.Weight"/> based on the player's current
    /// <b>science point total</b>.
    ///
    /// <para>
    /// The x-axis is <c>ScienceService.SciencePoints</c> (an integer).
    /// <see cref="CurveKeyframe.Value"/> is reused as the x-axis value.
    /// </para>
    ///
    /// Example — make science rewards less likely once the player is already rich:
    /// <code>
    /// "ScienceCurve": {
    ///   "Keyframes": [
    ///     { "Value": 0,    "Multiplier": 1.0 },
    ///     { "Value": 500,  "Multiplier": 1.0 },
    ///     { "Value": 1000, "Multiplier": 0.2 }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    public record ScienceCurve
    {
        [Serialize] public ImmutableArray<CurveKeyframe> Keyframes { get; init; }

        private ImmutableArray<CurveKeyframe>? _sorted;
        private ImmutableArray<CurveKeyframe> Sorted =>
            _sorted ??= Keyframes.IsDefaultOrEmpty
                ? ImmutableArray<CurveKeyframe>.Empty
                : Keyframes.OrderBy(k => k.Value).ToImmutableArray();

        /// <summary>
        /// Evaluates the multiplier for the given <paramref name="sciencePoints"/> total.
        /// Returns 1 if no keyframes are defined.
        /// </summary>
        public float Evaluate(float sciencePoints)
        {
            ImmutableArray<CurveKeyframe> keys = Sorted;
            if (keys.IsEmpty) return 1f;
            if (sciencePoints <= keys[0].Value) return keys[0].Multiplier;
            int last = keys.Length - 1;
            if (sciencePoints >= keys[last].Value) return keys[last].Multiplier;
            for (int i = 0; i < last; i++)
            {
                CurveKeyframe a = keys[i];
                CurveKeyframe b = keys[i + 1];
                if (sciencePoints >= a.Value && sciencePoints <= b.Value)
                {
                    float t = (sciencePoints - a.Value) / (b.Value - a.Value);
                    return a.Multiplier + t * (b.Multiplier - a.Multiplier);
                }
            }
            return keys[last].Multiplier;
        }
    }
}
