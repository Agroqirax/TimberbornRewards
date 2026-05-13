using System.Collections.Generic;
using Timberborn.BlueprintSystem;
using Timberborn.Goods;
using UnityEngine;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Lazy lookup from good ID to <see cref="GoodSpec"/>.
    /// Indexed once on first access; covers vanilla goods and any mod-added goods.
    /// </summary>
    public class GoodSpecRepository
    {
        private readonly ISpecService _specService;
        private Dictionary<string, GoodSpec>? _index;

        public GoodSpecRepository(ISpecService specService)
        {
            _specService = specService;
        }

        /// <summary>Returns the GoodSpec for <paramref name="goodId"/>, or null if not found.</summary>
        public GoodSpec? Get(string goodId)
        {
            _index ??= BuildIndex();
            return _index.TryGetValue(goodId, out GoodSpec? spec) ? spec : null;
        }

        private Dictionary<string, GoodSpec> BuildIndex()
        {
            var index = new Dictionary<string, GoodSpec>();
            foreach (GoodSpec spec in _specService.GetSpecs<GoodSpec>())
            {
                if (string.IsNullOrEmpty(spec.Id))
                    continue;
                if (!index.TryAdd(spec.Id, spec))
                    Debug.LogWarning($"[CycleBenefit] Duplicate GoodSpec Id '{spec.Id}' — keeping first.");
            }
            return index;
        }
    }
}
