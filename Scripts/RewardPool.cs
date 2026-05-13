using System.Collections.Generic;
using Timberborn.BlueprintSystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.ScienceSystem;
using UnityEngine;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// Builds and holds the list of unique weighted rewards for the current faction.
    /// Each entry pairs an <see cref="IReward"/> with its configured weight; the
    /// draw algorithm in <see cref="CycleRewardService"/> uses weights directly
    /// rather than expanding them into repeated pool entries, which guarantees that
    /// the same reward is never offered twice in one draw.
    /// </summary>
    public class RewardPool
    {
        private readonly ISpecService            _specService;
        private readonly ScienceService          _scienceService;
        private readonly DistrictCenterRegistry  _districtCenterRegistry;
        private readonly GoodSpecRepository      _goodSpecRepository;

        private List<(IReward Reward, int Weight)>? _pool;

        public RewardPool(
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
        /// Unique weighted reward entries. Empty until <see cref="InitForFaction"/> is called.
        /// </summary>
        public IReadOnlyList<(IReward Reward, int Weight)> UniqueWeighted =>
            _pool ??= new List<(IReward, int)>();

        /// <summary>
        /// Builds the pool for the given faction ID.
        /// Returns <c>true</c> if at least one reward was loaded.
        /// </summary>
        public bool InitForFaction(string factionId)
        {
            _pool = BuildPool(factionId);
            return _pool.Count > 0;
        }

        private List<(IReward, int)> BuildPool(string factionId)
        {
            FactionRewardSpec? spec = FindSpec(factionId);
            if (spec == null)
            {
                Debug.LogWarning(
                    $"[CycleReward] No FactionRewardSpec found for faction '{factionId}'. " +
                    $"Create Configurations/Rewards.{factionId}.blueprint.json to add support.");
                return new List<(IReward, int)>();
            }

            var pool = new List<(IReward, int)>();
            foreach (RewardEntrySpec entry in spec.Rewards)
            {
                IReward? reward = CreateReward(entry);
                if (reward == null)
                    continue;

                pool.Add((reward, entry.Weight > 0 ? entry.Weight : 1));
            }

            Debug.Log($"[CycleReward] Loaded {pool.Count} unique rewards for faction '{factionId}'.");
            return pool;
        }

        private FactionRewardSpec? FindSpec(string factionId)
        {
            foreach (FactionRewardSpec spec in _specService.GetSpecs<FactionRewardSpec>())
                if (spec.FactionId == factionId)
                    return spec;
            return null;
        }

        private IReward? CreateReward(RewardEntrySpec entry)
        {
            switch (entry.Type)
            {
                case "Science":
                    return new SciencePointReward(_scienceService, entry.Amount);

                case "Resource":
                {
                    GoodSpec? goodSpec = _goodSpecRepository.Get(entry.GoodId);
                    if (goodSpec == null)
                    {
                        Debug.LogWarning(
                            $"[CycleReward] Unknown GoodId '{entry.GoodId}' — skipping entry.");
                        return null;
                    }
                    return new ResourceReward(
                        _districtCenterRegistry,
                        goodId:      entry.GoodId,
                        amount:      entry.Amount,
                        displayName: goodSpec.DisplayName.Value,
                        iconPath:    goodSpec.Icon.Path);
                }

                default:
                    Debug.LogWarning(
                        $"[CycleReward] Unknown reward type '{entry.Type}' — skipping entry.");
                    return null;
            }
        }
    }
}