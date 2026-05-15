using System.Collections.Generic;
using Timberborn.BlueprintSystem;
using Timberborn.GameCycleSystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.HazardousWeatherSystem;
using Timberborn.ScienceSystem;
using Timberborn.WeatherSystem;
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
        private readonly ISpecService                    _specService;
        private readonly ScienceService                  _scienceService;
        private readonly DistrictCenterRegistry          _districtCenterRegistry;
        private readonly GoodSpecRepository              _goodSpecRepository;
        private readonly TemperateWeatherDurationService _temperateWeatherDurationService;
        private readonly HazardousWeatherService         _hazardousWeatherService;
        private readonly GameCycleService                _gameCycleService;

        /// <summary>
        /// Pairs of (reward, entry-spec) loaded for the current faction.
        /// Empty until <see cref="InitForFaction"/> is called.
        /// </summary>
        private List<(IReward Reward, RewardEntrySpec Entry)> _entries
            = new List<(IReward, RewardEntrySpec)>();

        public RewardPool(
            ISpecService                    specService,
            ScienceService                  scienceService,
            DistrictCenterRegistry          districtCenterRegistry,
            GoodSpecRepository              goodSpecRepository,
            TemperateWeatherDurationService temperateWeatherDurationService,
            HazardousWeatherService         hazardousWeatherService,
            GameCycleService                gameCycleService)
        {
            _specService                     = specService;
            _scienceService                  = scienceService;
            _districtCenterRegistry          = districtCenterRegistry;
            _goodSpecRepository              = goodSpecRepository;
            _temperateWeatherDurationService = temperateWeatherDurationService;
            _hazardousWeatherService         = hazardousWeatherService;
            _gameCycleService                = gameCycleService;
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
                            $"[CycleReward] Unknown GoodId '{entry.GoodId}' -- skipping entry.");
                        return null;
                    }
                    return new ResourceReward(
                        _districtCenterRegistry,
                        goodId:      entry.GoodId,
                        amount:      entry.Amount,
                        displayName: goodSpec.DisplayName.Value,
                        iconPath:    goodSpec.Icon.Path);
                }

                case "Weather":
                    return CreateWeatherReward(entry);

                default:
                    Debug.LogWarning(
                        $"[CycleReward] Unknown reward type '{entry.Type}' -- skipping entry.");
                    return null;
            }
        }

        private IReward? CreateWeatherReward(RewardEntrySpec entry)
        {
            if (entry.Amount == 0)
            {
                Debug.LogWarning("[CycleReward] Weather reward has DeltaDays 0 -- skipping entry.");
                return null;
            }

            WeatherType season;
            switch (entry.Season)
            {
                case "Temperate":
                    season = WeatherType.Temperate;
                    break;
                case "Hazardous":
                    season = WeatherType.Hazardous;
                    break;
                default:
                    Debug.LogWarning(
                        $"[CycleReward] Weather reward has unknown Season '{entry.Season}' " +
                        "(expected \"Temperate\" or \"Hazardous\") -- skipping entry.");
                    return null;
            }

            return new WeatherReward(
                _temperateWeatherDurationService,
                _hazardousWeatherService,
                _gameCycleService,
                season,
                entry.Amount);
        }
    }
}
