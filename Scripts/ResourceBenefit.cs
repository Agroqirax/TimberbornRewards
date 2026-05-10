using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.Localization;
using Timberborn.SimpleOutputBuildings;
using UnityEngine;

namespace Agroqirax.Benefits
{
    public class ResourceBenefit : IBenefit
    {
        private static readonly string LocKey = "CycleBenefit.Resource";

        private readonly DistrictCenterRegistry _districtCenterRegistry;
        private readonly string _goodId;
        private readonly int _amount;
        private readonly string _iconPath;

        public string? IconPath => _iconPath;

        public ResourceBenefit(DistrictCenterRegistry districtCenterRegistry,
                               string goodId,
                               int amount,
                               string iconPath)
        {
            _districtCenterRegistry = districtCenterRegistry;
            _goodId                 = goodId;
            _amount                 = amount;
            _iconPath               = iconPath;
        }

        public string GetDisplayName(ILoc loc) => loc.T(LocKey, _amount, _goodId);

        public void Apply()
        {
            DistrictCenter? largest = FindLargestFinishedDistrict();
            if (largest == null)
            {
                Debug.LogWarning("[CycleBenefit] No finished district center found — resource benefit lost.");
                return;
            }

            SimpleOutputInventory outputInventory = largest.GetComponent<SimpleOutputInventory>();
            if (outputInventory == null)
            {
                Debug.LogWarning($"[CycleBenefit] District center {largest.DistrictName} " +
                                 "has no SimpleOutputInventory — resource benefit lost.");
                return;
            }

            var good = new GoodAmount(_goodId, _amount);
            outputInventory.Inventory.GiveIgnoringCapacity(good);
            Debug.Log($"[CycleBenefit] Gave {_amount}x {_goodId} to {largest.DistrictName}.");
        }

        private DistrictCenter? FindLargestFinishedDistrict()
        {
            DistrictCenter? best    = null;
            int             bestPop = -1;

            foreach (DistrictCenter dc in _districtCenterRegistry.FinishedDistrictCenters)
            {
                int pop = dc.DistrictPopulation.NumberOfAdults
                        + dc.DistrictPopulation.NumberOfChildren
                        + dc.DistrictPopulation.NumberOfBots;
                if (pop > bestPop)
                {
                    bestPop = pop;
                    best    = dc;
                }
            }

            return best;
        }
    }
}