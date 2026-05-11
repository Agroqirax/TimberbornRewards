using System.Collections.Generic;
using Timberborn.BlueprintSystem;
using Timberborn.Goods;
using UnityEngine;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Builds a goodId → GoodSpec lookup from every GoodSpec loaded by the
    /// game (vanilla + mods).  Inject this wherever you need a good's
    /// localization key or icon path.
    /// </summary>
    public class GoodSpecRepository
    {
        private readonly ISpecService _specService;
        private Dictionary<string, GoodSpec>? _index;

        public GoodSpecRepository(ISpecService specService)
        {
            _specService = specService;
        }

        /// <summary>
        /// Returns the GoodSpec for the given ID, or null if not found.
        /// The index is built lazily on first access.
        /// </summary>
        public GoodSpec? Get(string goodId)
        {
            _index ??= BuildIndex();
            return _index.TryGetValue(goodId, out var spec) ? spec : null;
        }

        private Dictionary<string, GoodSpec> BuildIndex()
        {
            var index = new Dictionary<string, GoodSpec>();
            foreach (GoodSpec spec in _specService.GetSpecs<GoodSpec>())
            {
                if (string.IsNullOrEmpty(spec.Id))
                {
                    Debug.LogWarning("[CycleBenefit] Skipping GoodSpec with empty Id.");
                    continue;
                }
                if (!index.TryAdd(spec.Id, spec))
                    Debug.LogWarning($"[CycleBenefit] Duplicate GoodSpec Id '{spec.Id}' — keeping first.");
            }
            Debug.Log($"[CycleBenefit] GoodSpecRepository indexed {index.Count} goods.");
            return index;
        }
    }
}
