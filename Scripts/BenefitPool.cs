using System.Collections.Generic;
using Timberborn.BlueprintSystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.ScienceSystem;
using UnityEngine;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Builds the weighted pool of <see cref="IBenefit"/> instances for the current faction.
    /// Each entry with Weight N is added N times so that random sampling via a simple
    /// index pick produces the correct distribution.
    /// </summary>
    public class BenefitPool
    {
        private readonly ISpecService            _specService;
        private readonly ScienceService          _scienceService;
        private readonly DistrictCenterRegistry  _districtCenterRegistry;
        private readonly GoodSpecRepository      _goodSpecRepository;

        private List<IBenefit>? _pool;

        public BenefitPool(
            ISpecService           specService,
            ScienceService         scienceService,
            DistrictCenterRegistry districtCenterRegistry,
            GoodSpecRepository     goodSpecRepository)
        {
            _specService            = specService;
            _scienceService         = scienceService;
            _districtCenterRegistry = districtCenterRegistry;
            _goodSpecRepository     = goodSpecRepository;
        }

        /// <summary>The built pool. Empty until <see cref="InitForFaction"/> is called.</summary>
        public IReadOnlyList<IBenefit> All => _pool ??= new List<IBenefit>();

        /// <summary>
        /// Builds the pool for the given faction ID.
        /// Returns <c>true</c> if at least one benefit was loaded.
        /// </summary>
        public bool InitForFaction(string factionId)
        {
            _pool = BuildPool(factionId);
            return _pool.Count > 0;
        }

        private List<IBenefit> BuildPool(string factionId)
        {
            FactionBenefitSpec? spec = FindSpec(factionId);
            if (spec == null)
            {
                Debug.LogWarning(
                    $"[CycleBenefit] No FactionBenefitSpec found for faction '{factionId}'. " +
                    $"Create Configurations/Benefits.{factionId}.blueprint.json to add support.");
                return new List<IBenefit>();
            }

            var pool = new List<IBenefit>();
            foreach (BenefitEntrySpec entry in spec.Benefits)
            {
                IBenefit? benefit = CreateBenefit(entry);
                if (benefit == null)
                    continue;

                int weight = entry.Weight > 0 ? entry.Weight : 1;
                for (int i = 0; i < weight; i++)
                    pool.Add(benefit);
            }

            Debug.Log($"[CycleBenefit] Loaded {pool.Count} weighted pool entries for faction '{factionId}'.");
            return pool;
        }

        private FactionBenefitSpec? FindSpec(string factionId)
        {
            foreach (FactionBenefitSpec spec in _specService.GetSpecs<FactionBenefitSpec>())
                if (spec.FactionId == factionId)
                    return spec;
            return null;
        }

        private IBenefit? CreateBenefit(BenefitEntrySpec entry)
        {
            switch (entry.Type)
            {
                case "Science":
                    return new SciencePointBenefit(_scienceService, entry.Amount);

                case "Resource":
                {
                    GoodSpec? goodSpec = _goodSpecRepository.Get(entry.GoodId);
                    if (goodSpec == null)
                    {
                        Debug.LogWarning(
                            $"[CycleBenefit] Unknown GoodId '{entry.GoodId}' — skipping entry.");
                        return null;
                    }
                    return new ResourceBenefit(
                        _districtCenterRegistry,
                        goodId:      entry.GoodId,
                        amount:      entry.Amount,
                        displayName: goodSpec.DisplayName.Value,
                        iconPath:    goodSpec.Icon.Path);
                }

                default:
                    Debug.LogWarning($"[CycleBenefit] Unknown benefit type '{entry.Type}' — skipping entry.");
                    return null;
            }
        }
    }
}
