#nullable enable
using Timberborn.Beavers;
using Timberborn.Bots;
using Timberborn.GameDistricts;
using Timberborn.Localization;
using Timberborn.Navigation;
using UnityEngine;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A reward that spawns a number of new beavers (adults), beaver children,
    /// or bots at the largest finished district center.
    ///
    /// <para>
    /// Spawning via <see cref="BeaverFactory.CreateNewbornAdult"/>,
    /// <see cref="BeaverFactory.CreateNewbornChild"/>, or
    /// <see cref="BotFactory.Create"/> fires <c>CharacterCreatedEvent</c>
    /// internally, which registers the character with <c>BeaverPopulation</c> /
    /// <c>BotPopulation</c> and <c>UnassignedCitizenRegistry</c>. On the next
    /// tick <c>DistrictCitizenAssigner</c> picks up the unassigned citizens and
    /// assigns them to the nearest reachable district automatically — no manual
    /// district assignment is needed here.
    /// </para>
    ///
    /// <para>
    /// All characters are spawned at the district center's doorstep world
    /// position with a small random horizontal offset so they don't all stack
    /// on the same tile.
    /// </para>
    /// </summary>
    public class PopulationReward : IReward
    {
        private static readonly string BeaverSingularLocKey = "CycleReward.Population.Beaver.DisplayName";
        private static readonly string BeaverPluralLocKey   = "CycleReward.Population.Beaver.PluralDisplayName";
        private static readonly string ChildSingularLocKey  = "CycleReward.Population.Child.DisplayName";
        private static readonly string ChildPluralLocKey    = "CycleReward.Population.Child.PluralDisplayName";
        private static readonly string BotSingularLocKey    = "CycleReward.Population.Bot.DisplayName";
        private static readonly string BotPluralLocKey      = "CycleReward.Population.Bot.PluralDisplayName";

        // Vanilla icon paths — same pattern used by NeedReward / SciencePointReward.
        // Children share the adult population icon; there is no separate child icon in vanilla.
        private static readonly string BeaverIconPath_ = "ui/images/game/ico-beavers";
        private static readonly string ChildIconPath_  = "ui/images/game/ico-child";
        private static readonly string BotIconPath_    = "ui/images/game/ico-bot";

        // Spread radius so spawned characters don't all occupy the same tile.
        private const float SpawnSpread = 0.4f;

        private readonly BeaverFactory             _beaverFactory;
        private readonly BotFactory                _botFactory;
        private readonly DistrictCenterRegistry    _districtCenterRegistry;
        private readonly PopulationCharacterTarget _target;
        private readonly int                       _count;
        private readonly System.Random             _random;

        public string? IconPath => _target switch
        {
            PopulationCharacterTarget.Bot   => BotIconPath_,
            PopulationCharacterTarget.Child => ChildIconPath_,
            _                               => BeaverIconPath_,
        };

        public PopulationReward(
            BeaverFactory             beaverFactory,
            BotFactory                botFactory,
            DistrictCenterRegistry    districtCenterRegistry,
            PopulationCharacterTarget target,
            int                       count,
            System.Random             random)
        {
            _beaverFactory          = beaverFactory;
            _botFactory             = botFactory;
            _districtCenterRegistry = districtCenterRegistry;
            _target                 = target;
            _count                  = count;
            _random                 = random;
        }

        public string GetDisplayName(ILoc loc)
        {
            bool   plural = _count != 1;
            string signed = _count > 0 ? $"+{_count}" : _count.ToString();
            string locKey = _target switch
            {
                PopulationCharacterTarget.Bot   => plural ? BotPluralLocKey   : BotSingularLocKey,
                PopulationCharacterTarget.Child => plural ? ChildPluralLocKey : ChildSingularLocKey,
                _                               => plural ? BeaverPluralLocKey : BeaverSingularLocKey,
            };
            return loc.T(locKey, signed);
        }

        public void Apply()
        {
            DistrictCenter? district = FindLargestDistrict();
            if (district == null)
            {
                Debug.LogWarning("[CycleReward] Population reward: no finished district center found — reward lost.");
                return;
            }

            // Convert grid doorstep coordinates to world space, same as
            // DistrictCitizenAssigner does when measuring reachability.
            Vector3 origin = NavigationCoordinateSystem.GridToWorld(district.CenterCoordinates);

            for (int i = 0; i < _count; i++)
            {
                Vector3 spawnPos = origin + RandomOffset();

                switch (_target)
                {
                    case PopulationCharacterTarget.Bot:
                        _botFactory.Create(spawnPos);
                        break;
                    case PopulationCharacterTarget.Child:
                        _beaverFactory.CreateNewbornChild(spawnPos);
                        break;
                    default:
                        _beaverFactory.CreateNewbornAdult(spawnPos);
                        break;
                }
            }

            string typeName = _target switch
            {
                PopulationCharacterTarget.Bot   => "bot(s)",
                PopulationCharacterTarget.Child => "child(ren)",
                _                               => "beaver(s)",
            };
            Debug.Log($"[CycleReward] Spawned {_count} {typeName} at district '{district.DistrictName}'.");
        }

        // -----------------------------------------------------------------------

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

        /// <summary>Small random XZ offset so spawned characters don't overlap.</summary>
        private Vector3 RandomOffset()
        {
            float x = (float)(_random.NextDouble() * 2.0 - 1.0) * SpawnSpread;
            float z = (float)(_random.NextDouble() * 2.0 - 1.0) * SpawnSpread;
            return new Vector3(x, 0f, z);
        }
    }
}
