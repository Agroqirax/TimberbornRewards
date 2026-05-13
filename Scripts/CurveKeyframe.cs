using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A single keyframe in a <see cref="WeightCurve"/>.
    /// </summary>
    public record CurveKeyframe
    {
        /// <summary>The cycle number at which this multiplier applies exactly.</summary>
        [Serialize] public int   Cycle      { get; init; }

        /// <summary>
        /// Weight multiplier at this cycle. Values between keyframes are linearly
        /// interpolated. Clamped to the first/last keyframe outside the defined range.
        /// </summary>
        [Serialize] public float Multiplier { get; init; } = 1f;
    }
}
