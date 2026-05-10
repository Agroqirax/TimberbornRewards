using System.Collections.Generic;
using Timberborn.BlueprintSystem;
using Timberborn.GameDistricts;
using Timberborn.ScienceSystem;
using UnityEngine;

namespace Agroqirax.Benefits
{
    public class BenefitPool
    {
        private readonly ISpecService _specService;
        private readonly ScienceService _scienceService;
        private readonly DistrictCenterRegistry _districtCenterRegistry;

        private List<IBenefit>? _pool;

        public BenefitPool(ISpecService specService,
                           ScienceService scienceService,
                           DistrictCenterRegistry districtCenterRegistry)
        {
            _specService            = specService;
            _scienceService         = scienceService;
            _districtCenterRegistry = districtCenterRegistry;
        }

        public IReadOnlyList<IBenefit> All => _pool ??= BuildPool();

        private List<IBenefit> BuildPool()
        {
            var pool = new List<IBenefit>();
            BenefitSpec spec = _specService.GetSingleSpec<BenefitSpec>();

            foreach (BenefitEntrySpec entry in spec.Benefits)
            {
                IBenefit? benefit = CreateBenefit(entry);
                if (benefit == null) continue;

                int weight = entry.Weight > 0 ? entry.Weight : 1;
                for (int i = 0; i < weight; i++)
                    pool.Add(benefit);
            }

            return pool;
        }

        private IBenefit? CreateBenefit(BenefitEntrySpec entry)
        {
            switch (entry.Type)
            {
                case "Science":
                    return new SciencePointBenefit(_scienceService, entry.Amount);

                case "Resource":
                    return new ResourceBenefit(
                        _districtCenterRegistry,
                        entry.GoodId,
                        entry.Amount,
                        entry.IconPath);

                default:
                    Debug.LogWarning($"[CycleBenefit] Unknown benefit type '{entry.Type}' — skipping.");
                    return null;
            }
        }
    }
}