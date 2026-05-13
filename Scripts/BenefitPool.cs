using System.Collections.Generic;
using Timberborn.BlueprintSystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.ScienceSystem;
using UnityEngine;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Builds and holds the list of unique weighted benefits for the current faction.
    /// Each entry pairs an <see cref="IBenefit"/> with its configured weight; the
    /// draw algorithm in <see cref="CycleBenefitService"/> uses weights directly
    /// rather than expanding them into repeated pool entries, which guarantees that
    /// the same benefit is never offered twice in one draw.
    /// </summary>
    public class BenefitPool
    {
        private readonly ISpecService            _specService;
        private readonly ScienceService          _scienceService;
        private readonly DistrictCenterRegistry  _districtCenterRegistry;
        private readonly GoodSpecRepository      _goodSpecRepository;

        private List<(IBenefit Benefit, int Weight)>? _pool;

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

        /// <summary>
        /// Unique weighted benefit entries. Empty until <see cref="InitForFaction"/> is called.
        /// </summary>
        public IReadOnlyList<(IBenefit Benefit, int Weight)> UniqueWeighted =>
            _pool ??= new List<(IBenefit, int)>();

        /// <summary>
        /// Builds the pool for the given faction ID.
        /// Returns <c>true</c> if at least one benefit was loaded.
        /// </summary>
        public bool InitForFaction(string factionId)
        {
            _pool = BuildPool(factionId);
            return _pool.Count > 0;
        }

        private List<(IBenefit, int)> BuildPool(string factionId)
        {
            FactionBenefitSpec? spec = FindSpec(factionId);
            if (spec == null)
            {
                Debug.LogWarning(
                    $"[CycleBenefit] No FactionBenefitSpec found for faction '{factionId}'. " +
                    $"Create Configurations/Benefits.{factionId}.blueprint.json to add support.");
                return new List<(IBenefit, int)>();
            }

            var pool = new List<(IBenefit, int)>();
            foreach (BenefitEntrySpec entry in spec.Benefits)
            {
                IBenefit? benefit = CreateBenefit(entry);
                if (benefit == null)
                    continue;

                pool.Add((benefit, entry.Weight > 0 ? entry.Weight : 1));
            }

            Debug.Log($"[CycleBenefit] Loaded {pool.Count} unique benefits for faction '{factionId}'.");
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
                    Debug.LogWarning(
                        $"[CycleBenefit] Unknown benefit type '{entry.Type}' — skipping entry.");
                    return null;
            }
        }
    }
}