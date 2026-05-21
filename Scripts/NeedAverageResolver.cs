#nullable enable
using System.Collections.Generic;
using Timberborn.GameDistricts;
using Timberborn.NeedSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// Lazily resolves the global average need points (multiplied by 100 for use as
    /// integer-friendly keyframe values) for need IDs on demand.
    ///
    /// <para>
    /// Iterates all finished district centers, queries every character's
    /// <see cref="NeedManager"/> for <c>GetNeedPoints</c>, then averages the result.
    /// Characters that do not have the need are skipped. Bots and beavers are
    /// included — the need spec itself determines which character types have it.
    /// </para>
    ///
    /// <para>
    /// Results are cached per need ID within a single draw call.
    /// </para>
    /// </summary>
    public class NeedAverageResolver
    {
        private readonly DistrictCenterRegistry      _districtCenterRegistry;
        private readonly Dictionary<string, float>   _cache = new();

        public NeedAverageResolver(DistrictCenterRegistry districtCenterRegistry)
        {
            _districtCenterRegistry = districtCenterRegistry;
        }

        /// <summary>
        /// Returns the average need points for <paramref name="needId"/> across all
        /// living characters, multiplied by 100.
        /// Returns 1.0 (full) if no characters have the need (safe default — avoids
        /// incorrectly boosting relief rewards when data is absent).
        /// </summary>
        public float Get(string needId)
        {
            if (string.IsNullOrEmpty(needId))
                return 100f;

            if (_cache.TryGetValue(needId, out float cached))
                return cached;

            float sum   = 0f;
            int   count = 0;

            foreach (DistrictCenter dc in _districtCenterRegistry.FinishedDistrictCenters)
            {
                DistrictPopulation pop = dc.DistrictPopulation;

                foreach (var beaver in pop.Beavers)
                {
                    NeedManager? nm = beaver.GetComponent<NeedManager>();
                    if (nm != null && nm.HasNeed(needId))
                    {
                        sum += nm.GetNeedPoints(needId);
                        count++;
                    }
                }

                foreach (var bot in pop.Bots)
                {
                    NeedManager? nm = bot.GetComponent<NeedManager>();
                    if (nm != null && nm.HasNeed(needId))
                    {
                        sum += nm.GetNeedPoints(needId);
                        count++;
                    }
                }
            }

            float result = count == 0 ? 1f : sum / count;
            _cache[needId] = result;
            return result;
        }
    }
}
