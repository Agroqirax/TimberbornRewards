#nullable enable
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.Localization;
using Timberborn.SimpleOutputBuildings;
using UnityEngine;

namespace Agroqirax.Rewards
{
    public class ResourceReward : IReward
    {
        // "+{0} {1}" — {0} = amount, {1} = localized good name
        private static readonly string LocKey = "CycleReward.Resource";

        private readonly DistrictCenterRegistry _districtCenterRegistry;
        private readonly string  _goodId;
        private readonly int     _amount;
        private readonly string  _displayName;
        private readonly string  _pluralDisplayName;
        private readonly string? _iconPath;

        public string? IconPath => _iconPath;

        public ResourceReward(
            DistrictCenterRegistry districtCenterRegistry,
            string  goodId,
            int     amount,
            string  displayName,
            string  pluralDisplayName,
            string? iconPath)
        {
            _districtCenterRegistry = districtCenterRegistry;
            _goodId                 = goodId;
            _amount                 = amount;
            _displayName            = displayName;
            _pluralDisplayName      = pluralDisplayName;
            _iconPath               = iconPath;
        }

        public string GetDisplayName(ILoc loc)
        {
            string goodName = _amount == 1 ? _displayName : _pluralDisplayName;
            return loc.T(LocKey, _amount, goodName);
        }

        public void Apply()
        {
            DistrictCenter? district = FindLargestDistrict();
            if (district == null)
            {
                Debug.LogWarning("[CycleReward] No finished district center found — resource reward lost.");
                return;
            }

            SimpleOutputInventory? inventory = district.GetComponent<SimpleOutputInventory>();
            if (inventory == null)
            {
                Debug.LogWarning(
                    $"[CycleReward] District '{district.DistrictName}' has no SimpleOutputInventory " +
                    "— resource reward lost.");
                return;
            }

            inventory.Inventory.GiveIgnoringCapacity(new GoodAmount(_goodId, _amount));
            Debug.Log($"[CycleReward] Gave {_amount}x {_goodId} to '{district.DistrictName}'.");
        }

        private DistrictCenter? FindLargestDistrict()
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