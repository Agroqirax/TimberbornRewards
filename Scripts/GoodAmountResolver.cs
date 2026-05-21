using System.Collections.Generic;
using Timberborn.ResourceCountingSystem;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// Lazily resolves the global available stock for good IDs on demand.
    /// Caches results within a single draw call so the same good is never
    /// queried from <see cref="ResourceCountingService"/> twice.
    /// </summary>
    public class GoodAmountResolver
    {
        private readonly ResourceCountingService          _resourceCountingService;
        private readonly Dictionary<string, int>          _cache = new();

        public GoodAmountResolver(ResourceCountingService resourceCountingService)
        {
            _resourceCountingService = resourceCountingService;
        }

        /// <summary>
        /// Returns the global available stock of <paramref name="goodId"/>
        /// (the same value shown in the top bar).
        /// Returns 0 if the good ID is null or empty.
        /// </summary>
        public int Get(string goodId)
        {
            if (string.IsNullOrEmpty(goodId))
                return 0;

            if (_cache.TryGetValue(goodId, out int cached))
                return cached;

            int amount = _resourceCountingService.GetGlobalResourceCount(goodId).AvailableStock;
            _cache[goodId] = amount;
            return amount;
        }
    }
}
