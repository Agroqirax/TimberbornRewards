using Timberborn.BlueprintSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A single keyframe shared by all curve types
    /// (<see cref="CycleCurve"/>, <see cref="GoodCurve"/>, <see cref="NeedCurve"/>,
    /// <see cref="ScienceCurve"/>, <see cref="PopulationCurve"/>).
    ///
    /// <para>
    /// <see cref="Value"/> is the x-axis position of this keyframe. Its meaning
    /// depends on the curve type that owns it:
    /// <list type="bullet">
    ///   <item><see cref="CycleCurve"/>      — game cycle number (integer)</item>
    ///   <item><see cref="GoodCurve"/>       — global available stock of the good (integer items)</item>
    ///   <item><see cref="NeedCurve"/>       — average need points × 100, so 0–100 for a [0,1] need</item>
    ///   <item><see cref="ScienceCurve"/>    — current science point total (integer)</item>
    ///   <item><see cref="PopulationCurve"/> — total character count across all districts (integer)</item>
    /// </list>
    /// </para>
    /// </summary>
    public record CurveKeyframe
    {
        /// <summary>
        /// The x-axis position at which this multiplier applies exactly.
        /// Values between keyframes are linearly interpolated; outside the
        /// defined range the nearest keyframe's multiplier is clamped.
        /// </summary>
        [Serialize] public float Value      { get; init; }

        /// <summary>Weight multiplier at this keyframe value.</summary>
        [Serialize] public float Multiplier { get; init; } = 1f;
    }
}
