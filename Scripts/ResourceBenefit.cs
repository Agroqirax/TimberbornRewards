using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.Localization;
using Timberborn.SimpleOutputBuildings;
using UnityEngine;

namespace Agroqirax.Benefits
{
    public class ResourceBenefit : IBenefit
    {
        // Format: "+{0} {1}" where {0}=amount, {1}=good display name.
        private static readonly string LocKey = "CycleBenefit.Resource";

        private readonly DistrictCenterRegistry _districtCenterRegistry;
        private readonly string  _goodId;
        private readonly int     _amount;
        /// <summary>Already-resolved display name from GoodSpec.DisplayName.Value.</summary>
        private readonly string? _displayName;
        private readonly string? _iconPath;

        public string? IconPath => _iconPath;

        public ResourceBenefit(
            DistrictCenterRegistry districtCenterRegistry,
            string  goodId,
            int     amount,
            string? displayName,
            string? iconPath)
        {
            _districtCenterRegistry = districtCenterRegistry;
            _goodId      = goodId;
            _amount      = amount;
            _displayName = displayName;
            _iconPath    = iconPath;
        }

        public string GetDisplayName(ILoc loc)
        {
            // _displayName is already the translated good name (e.g. "Gear"),
            // so pass it directly as the {1} argument — no second loc.T() call.
            string goodName = !string.IsNullOrEmpty(_displayName) ? _displayName : _goodId;
            return loc.T(LocKey, _amount, goodName);
        }

        public void Apply()
        {
            DistrictCenter? largest = FindLargestFinishedDistrict();
            if (largest == null)
            {
                Debug.LogWarning("[CycleBenefit] No finished district center found — resource benefit lost.");
                return;
            }

            SimpleOutputInventory? outputInventory = largest.GetComponent<SimpleOutputInventory>();
            if (outputInventory == null)
            {
                Debug.LogWarning(
                    $"[CycleBenefit] District center '{largest.DistrictName}' " +
                    "has no SimpleOutputInventory — resource benefit lost.");
                return;
            }

            outputInventory.Inventory.GiveIgnoringCapacity(new GoodAmount(_goodId, _amount));
            Debug.Log($"[CycleBenefit] Gave {_amount}x {_goodId} to '{largest.DistrictName}'.");
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