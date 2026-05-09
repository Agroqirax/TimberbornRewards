using System.Collections.Generic;
using Timberborn.ScienceSystem;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Holds every possible benefit that can be offered to the player.
    /// Add new benefit types here as the mod grows.
    ///
    /// This is a singleton injected by Bindito; it takes ScienceService as a
    /// dependency so it can construct SciencePointBenefits without the caller
    /// needing to know about the concrete type.
    /// </summary>
    public class BenefitPool
    {
        private readonly ScienceService _scienceService;

        // All benefits available in the pool.
        // Duplicates are intentional: a higher-value benefit can appear more
        // than once to adjust effective draw probability if desired.
        private List<IBenefit>? _pool;

        public BenefitPool(ScienceService scienceService)
        {
            _scienceService = scienceService;
        }

        /// <summary>
        /// Returns the full pool, constructing it lazily on first access.
        /// </summary>
        public IReadOnlyList<IBenefit> All => _pool ??= BuildPool();

        private List<IBenefit> BuildPool()
        {
            return new List<IBenefit>
            {
                // Science point tiers — small, medium, large
                new SciencePointBenefit(_scienceService, 50),
                new SciencePointBenefit(_scienceService, 50),   // two copies → slightly higher draw chance
                new SciencePointBenefit(_scienceService, 150),
                new SciencePointBenefit(_scienceService, 150),
                new SciencePointBenefit(_scienceService, 300),
                new SciencePointBenefit(_scienceService, 500),

                // More benefit types will go here once UI and resource rewards
                // are implemented (e.g. ResourceBenefit, HealBeaversBenefit, …)
            };
        }
    }
}
