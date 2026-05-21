#nullable enable
using System.Collections.Generic;
using System.Linq;
using Timberborn.BlueprintSystem;
using Timberborn.BlockObjectTools;
using Timberborn.Beavers;
using Timberborn.Bots;
using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.GameCycleSystem;
using Timberborn.GameDistricts;
using Timberborn.GameFactionSystem;
using Timberborn.Goods;
using Timberborn.HazardousWeatherSystem;
using Timberborn.NeedSpecs;
using Timberborn.ResourceCountingSystem;
using Timberborn.ScienceSystem;
using Timberborn.WeatherSystem;
using Timberborn.TemplateSystem;
using Timberborn.ToolButtonSystem;
using Timberborn.ToolSystem;
using UnityEngine;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// Builds and holds the list of rewards for the current faction.
    ///
    /// Weights are NOT baked at load time; call <see cref="GetWeightedForCycle"/>
    /// each draw so that all curve types can vary the effective weight (and
    /// eligibility) per draw based on current game state.
    ///
    /// For <c>Building</c> rewards, entries whose building is already unlocked are
    /// excluded from the draw entirely — they would be meaningless to offer.
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
        private readonly FactionNeedService              _factionNeedService;
        private readonly BuildingService                 _buildingService;
        private readonly BuildingUnlockingService        _buildingUnlockingService;
        private readonly ToolUnlockingService            _toolUnlockingService;
        private readonly ToolButtonService               _toolButtonService;
        private readonly BeaverFactory                   _beaverFactory;
        private readonly BotFactory                      _botFactory;
        private readonly ResourceCountingService         _resourceCountingService;

        // Shared Random instance passed down to rewards that need randomness.
        private readonly System.Random _random = new System.Random();

        /// <summary>Pairs of (reward, entry-spec) built for the current faction.</summary>
        private List<(IReward Reward, RewardEntrySpec Entry)> _entries
            = new List<(IReward, RewardEntrySpec)>();

        public RewardPool(
            ISpecService                    specService,
            ScienceService                  scienceService,
            DistrictCenterRegistry          districtCenterRegistry,
            GoodSpecRepository              goodSpecRepository,
            TemperateWeatherDurationService temperateWeatherDurationService,
            HazardousWeatherService         hazardousWeatherService,
            GameCycleService                gameCycleService,
            FactionNeedService              factionNeedService,
            BuildingService                 buildingService,
            BuildingUnlockingService        buildingUnlockingService,
            ToolUnlockingService            toolUnlockingService,
            ToolButtonService               toolButtonService,
            BeaverFactory                   beaverFactory,
            BotFactory                      botFactory,
            ResourceCountingService         resourceCountingService)
        {
            _specService                     = specService;
            _scienceService                  = scienceService;
            _districtCenterRegistry          = districtCenterRegistry;
            _goodSpecRepository              = goodSpecRepository;
            _temperateWeatherDurationService = temperateWeatherDurationService;
            _hazardousWeatherService         = hazardousWeatherService;
            _gameCycleService                = gameCycleService;
            _factionNeedService              = factionNeedService;
            _buildingService                 = buildingService;
            _buildingUnlockingService        = buildingUnlockingService;
            _toolUnlockingService            = toolUnlockingService;
            _toolButtonService               = toolButtonService;
            _beaverFactory                   = beaverFactory;
            _botFactory                      = botFactory;
            _resourceCountingService         = resourceCountingService;
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
        /// Entries are excluded when:
        /// <list type="bullet">
        ///   <item>their effective weight evaluates to &lt;= 0 for this draw, or</item>
        ///   <item>they are a <see cref="BuildingUnlockReward"/> for a building the
        ///         player has already unlocked.</item>
        /// </list>
        /// A <see cref="CurveContext"/> is built once per call and shared across
        /// all entry evaluations; resolver caches ensure each game-state query
        /// (good amount, need average, population count) is performed at most once.
        /// </summary>
        public List<(IReward Reward, float Weight)> GetWeightedForCycle(int cycle)
        {
            CurveContext context = BuildContext(cycle);

            var result = new List<(IReward, float)>(_entries.Count);
            foreach (var (reward, entry) in _entries)
            {
                float w = entry.GetWeightAt(context);
                if (w <= 0f)
                    continue;

                // Skip building unlocks that are already owned — pointless to offer.
                if (reward is BuildingUnlockReward
                    && IsAlreadyUnlocked(entry.BuildingTemplateName))
                    continue;

                result.Add((reward, w));
            }
            return result;
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Builds a <see cref="CurveContext"/> for the current frame.
        /// Resolver instances are created fresh each draw so their per-draw
        /// caches are clean.
        /// </summary>
        private CurveContext BuildContext(int cycle) => new CurveContext(
            cycle:         cycle,
            sciencePoints: _scienceService.SciencePoints,
            goods:         new GoodAmountResolver(_resourceCountingService),
            needs:         new NeedAverageResolver(_districtCenterRegistry),
            population:    new PopulationCountResolver(_districtCenterRegistry));

        private bool IsAlreadyUnlocked(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
                return false;
            try
            {
                BuildingSpec spec = _buildingService.GetBuildingTemplate(templateName);
                return _buildingUnlockingService.Unlocked(spec);
            }
            catch
            {
                return false;
            }
        }

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
                    return new SciencePointReward(_scienceService, UnityEngine.Mathf.RoundToInt(entry.Amount));

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
                        goodId:            entry.GoodId,
                        amount:            UnityEngine.Mathf.RoundToInt(entry.Amount),
                        displayName:       goodSpec.DisplayName.Value,
                        pluralDisplayName: goodSpec.PluralDisplayName.Value,
                        iconPath:          goodSpec.Icon.Path);
                }

                case "Weather":
                    return CreateWeatherReward(entry);

                case "Need":
                    return CreateNeedReward(entry);

                case "Building":
                    return CreateBuildingUnlockReward(entry);

                case "Population":
                    return CreatePopulationReward(entry);

                default:
                    Debug.LogWarning(
                        $"[CycleReward] Unknown reward type '{entry.Type}' — skipping entry.");
                    return null;
            }
        }

        private IReward? CreateWeatherReward(RewardEntrySpec entry)
        {
            if (entry.Amount == 0)
            {
                Debug.LogWarning("[CycleReward] Weather reward has Amount 0 — skipping entry.");
                return null;
            }

            WeatherType season;
            switch (entry.Season)
            {
                case "Temperate": season = WeatherType.Temperate; break;
                case "Hazardous": season = WeatherType.Hazardous; break;
                default:
                    Debug.LogWarning(
                        $"[CycleReward] Weather reward has unknown Season '{entry.Season}' " +
                        "(expected \"Temperate\" or \"Hazardous\") — skipping entry.");
                    return null;
            }

            return new WeatherReward(
                _temperateWeatherDurationService,
                _hazardousWeatherService,
                _gameCycleService,
                season,
                UnityEngine.Mathf.RoundToInt(entry.Amount));
        }

        private IReward? CreateNeedReward(RewardEntrySpec entry)
        {
            if (string.IsNullOrEmpty(entry.NeedId))
            {
                Debug.LogWarning("[CycleReward] Need reward is missing NeedId — skipping entry.");
                return null;
            }

            if (entry.Amount == 0f)
            {
                Debug.LogWarning(
                    $"[CycleReward] Need reward for '{entry.NeedId}' has Amount 0 — skipping entry.");
                return null;
            }

            var beaverNeeds = _factionNeedService.GetBeaverNeeds().ToList();
            var botNeeds    = _factionNeedService.GetBotNeeds().ToList();

            NeedSpec? needSpec =
                beaverNeeds.FirstOrDefault(n => n.Id == entry.NeedId)
                ?? botNeeds.FirstOrDefault(n => n.Id == entry.NeedId);

            if (needSpec == null)
            {
                Debug.LogWarning(
                    $"[CycleReward] NeedId '{entry.NeedId}' not found in beaver or bot needs — skipping entry. " +
                    $"Beaver need IDs: {string.Join(", ", beaverNeeds.Select(n => n.Id))} | " +
                    $"Bot need IDs: {string.Join(", ", botNeeds.Select(n => n.Id))}");
                return null;
            }

            NeedCharacterTarget target = needSpec.CharacterType == "Bot"
                ? NeedCharacterTarget.Bot
                : NeedCharacterTarget.Beaver;

            return new NeedReward(
                _districtCenterRegistry,
                entry.NeedId,
                entry.Amount,
                needSpec.DisplayNameLocKey,
                target);
        }

        private IReward? CreateBuildingUnlockReward(RewardEntrySpec entry)
        {
            if (string.IsNullOrEmpty(entry.BuildingTemplateName))
            {
                Debug.LogWarning(
                    "[CycleReward] Building reward is missing BuildingTemplateName — skipping entry.");
                return null;
            }

            BuildingSpec buildingSpec;
            try
            {
                buildingSpec = _buildingService.GetBuildingTemplate(entry.BuildingTemplateName);
            }
            catch
            {
                Debug.LogWarning(
                    $"[CycleReward] Building '{entry.BuildingTemplateName}' not found — skipping entry.");
                return null;
            }

            if (buildingSpec.ScienceCost == 0)
            {
                Debug.LogWarning(
                    $"[CycleReward] Building '{entry.BuildingTemplateName}' has no science cost " +
                    "(it is always available) — skipping entry.");
                return null;
            }

            LabeledEntitySpec? labelSpec = buildingSpec.GetSpec<LabeledEntitySpec>();
            if (labelSpec == null)
            {
                Debug.LogWarning(
                    $"[CycleReward] Building '{entry.BuildingTemplateName}' has no LabeledEntitySpec — skipping entry.");
                return null;
            }

            string? iconPath    = string.IsNullOrEmpty(labelSpec.Icon.Path) ? null : labelSpec.Icon.Path;
            string templateName = _buildingService.GetTemplateName(buildingSpec);

            return new BuildingUnlockReward(
                _buildingUnlockingService,
                _toolUnlockingService,
                _toolButtonService,
                buildingSpec,
                labelSpec.DisplayNameLocKey,
                templateName,
                iconPath);
        }

        private IReward? CreatePopulationReward(RewardEntrySpec entry)
        {
            int count = UnityEngine.Mathf.RoundToInt(entry.Amount);
            if (count <= 0)
            {
                Debug.LogWarning(
                    $"[CycleReward] Population reward has Amount {entry.Amount} (rounds to {count}) — " +
                    "must be a positive integer. Skipping entry.");
                return null;
            }

            PopulationCharacterTarget target;
            switch (entry.CharacterType)
            {
                case "Beaver": target = PopulationCharacterTarget.Beaver; break;
                case "Child":  target = PopulationCharacterTarget.Child;  break;
                case "Bot":    target = PopulationCharacterTarget.Bot;    break;
                default:
                    Debug.LogWarning(
                        $"[CycleReward] Population reward has unknown CharacterType '{entry.CharacterType}' " +
                        "(expected \"Beaver\", \"Child\", or \"Bot\") — skipping entry.");
                    return null;
            }

            return new PopulationReward(
                _beaverFactory,
                _botFactory,
                _districtCenterRegistry,
                target,
                count,
                _random);
        }
    }
}
