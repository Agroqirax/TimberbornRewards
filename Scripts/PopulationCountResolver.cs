#nullable enable
using System.Collections.Generic;
using Timberborn.BeaverContaminationSystem;
using Timberborn.GameDistricts;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// Lazily resolves the global population count for each
    /// <see cref="PopulationCurveTarget"/> on demand, caching results within
    /// a single draw call.
    ///
    /// <para>
    /// Contaminated beavers are detected by walking all beavers (adults and
    /// children) in each district and checking
    /// <c>Contaminable.IsContaminated</c> — replicating the logic of the
    /// internal <c>BeaverContaminationRegistry</c> without depending on it.
    /// </para>
    /// </summary>
    public class PopulationCountResolver
    {
        private readonly DistrictCenterRegistry              _districtCenterRegistry;
        private readonly Dictionary<PopulationCurveTarget, int> _cache = new();

        public PopulationCountResolver(DistrictCenterRegistry districtCenterRegistry)
        {
            _districtCenterRegistry = districtCenterRegistry;
        }

        /// <summary>
        /// Returns the total count of the given <paramref name="target"/> character
        /// type across all finished districts.
        /// </summary>
        public int Get(PopulationCurveTarget target)
        {
            if (_cache.TryGetValue(target, out int cached))
                return cached;

            int count = 0;
            foreach (DistrictCenter dc in _districtCenterRegistry.FinishedDistrictCenters)
            {
                DistrictPopulation pop = dc.DistrictPopulation;
                switch (target)
                {
                    case PopulationCurveTarget.Beaver:
                        count += pop.NumberOfAdults;
                        break;

                    case PopulationCurveTarget.Child:
                        count += pop.NumberOfChildren;
                        break;

                    case PopulationCurveTarget.Bot:
                        count += pop.NumberOfBots;
                        break;

                    case PopulationCurveTarget.Contaminated:
                        // Walk all beavers (adults + children) and check Contaminable.
                        // BeaverContaminationRegistry is internal so we replicate the
                        // logic here. Adults and Children are both in Beavers list via
                        // DistrictPopulation.Beavers (includes both), but to be explicit:
                        foreach (var beaver in pop.Beavers)
                        {
                            Contaminable? c = beaver.GetComponent<Contaminable>();
                            if (c != null && c.IsContaminated) count++;
                        }
                        break;
                }
            }

            _cache[target] = count;
            return count;
        }
    }
}
