using System.Collections.Generic;
using Timberborn.BlueprintSystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.ScienceSystem;
using UnityEngine;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// Builds and holds the list of rewards for the current faction.
    ///
    /// Weights are NOT baked at load time; call
    /// <see cref="GetWeightedForCycle"/> each draw so that
    /// <see cref="RewardEntrySpec.WeightCurve"/> can vary the effective weight
    /// (and eligibility) per cycle.
    /// </summary>
    public class RewardPool
    {
        private readonly ISpecService            _specService;
        private readonly ScienceService          _scienceService;
        private readonly DistrictCenterRegistry  _districtCenterRegistry;
        private readonly GoodSpecRepository      _goodSpecRepository;

        /// <summary>
        /// Pairs of (reward, entry-spec) loaded for the current faction.
        /// The entry-spec is kept so <see cref="GetWeightedForCycle"/> can
        /// evaluate the curve without re-parsing.
        /// Empty until <see cref="InitForFaction"/> is called.
        /// </summary>
        private List<(IReward Reward, RewardEntrySpec Entry)> _entries
            = new List<(IReward, RewardEntrySpec)>();

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
        /// Builds the pool for the given faction ID.
        /// Returns <c>true</c> if at least one reward was loaded.
        /// </summary>
        public bool InitForFaction(string factionId)
        {
            _entries = BuildEntries(factionId);
            return _entries.Count > 0;
        }

        /// <summary>
        /// Returns (reward, weight) pairs eligible for the given cycle.
        /// Entries whose effective weight evaluates to &lt;= 0 are excluded entirely.
        /// </summary>
        public List<(IReward Reward, float Weight)> GetWeightedForCycle(int cycle)
        {
            var result = new List<(IReward, float)>(_entries.Count);
            foreach (var (reward, entry) in _entries)
            {
                float w = entry.GetWeightAt(cycle);
                if (w > 0f)
                    result.Add((reward, w));
            }
            return result;
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private List<(IReward, RewardEntrySpec)> BuildEntries(string factionId)
        {
            FactionRewardSpec? spec = FindSpec(factionId);
            if (spec == null)
            {
                Debug.LogWarning(
                    $"[CycleReward] No FactionRewardSpec found for faction '{factionId}'. " +
                    $"Create Configurations/Rewards.{factionId}.blueprint.json to add support.");
                return new List<(IReward, RewardEntrySpec)>();
            }

            var entries = new List<(IReward, RewardEntrySpec)>();
            foreach (RewardEntrySpec entry in spec.Rewards)
            {
                IReward? reward = CreateReward(entry);
                if (reward != null)
                    entries.Add((reward, entry));
            }

            Debug.Log($"[CycleReward] Loaded {entries.Count} rewards for faction '{factionId}'.");
            return entries;
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
