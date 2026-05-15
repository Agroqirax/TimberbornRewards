#nullable enable
using Timberborn.Beavers;
using Timberborn.Bots;
using Timberborn.Effects;
using Timberborn.GameDistricts;
using Timberborn.Localization;
using Timberborn.NeedSystem;
using UnityEngine;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A reward that instantly adjusts a specific need for every living beaver
    /// or bot (determined by <see cref="NeedSpec.CharacterType"/> at build time).
    ///
    /// <para>
    /// <see cref="_amount"/> is the raw points delta on the need's own scale
    /// (e.g. Sleep lives in [-0.2, 0.8], so +0.5 is a substantial fill).
    /// Positive values fill the need; negative values drain it.
    /// Uses <see cref="NeedManager.ApplyEffect(InstantEffect)"/>, which fires
    /// all state-change events and respects <c>Wastable</c> and value clamping.
    /// </para>
    /// </summary>
    public class NeedReward : IReward
    {
        private static readonly string LocKey             = "CycleReward.Need";
        private static readonly string BeaverPluralLocKey = "Beaver.PluralDisplayName";
        private static readonly string BotPluralLocKey    = "Bot.PluralDisplayName";
        private static readonly string IconPath_          = "sprites/bottombar/buildinggroups/Wellbeing";

        private readonly DistrictCenterRegistry _districtCenterRegistry;
        private readonly string                 _needId;
        private readonly float                  _amount;
        private readonly string                 _needDisplayNameLocKey;
        private readonly NeedCharacterTarget    _target;

        public string? IconPath => IconPath_;

        public NeedReward(
            DistrictCenterRegistry districtCenterRegistry,
            string                 needId,
            float                  amount,
            string                 needDisplayNameLocKey,
            NeedCharacterTarget    target)
        {
            _districtCenterRegistry = districtCenterRegistry;
            _needId                 = needId;
            _amount                 = amount;
            _needDisplayNameLocKey  = needDisplayNameLocKey;
            _target                 = target;
        }

        public string GetDisplayName(ILoc loc)
        {
            string signed          = _amount > 0f ? $"+{_amount:0.##}" : $"{_amount:0.##}";
            string needName        = loc.T(_needDisplayNameLocKey);
            string characterPlural = loc.T(_target == NeedCharacterTarget.Bot
                ? BotPluralLocKey
                : BeaverPluralLocKey);
            return loc.T(LocKey, signed, needName, characterPlural);
        }

        public void Apply()
        {
            var effect = new InstantEffect(_needId, _amount, 1);
            int beaversAffected = 0;
            int botsAffected    = 0;

            foreach (DistrictCenter dc in _districtCenterRegistry.FinishedDistrictCenters)
            {
                DistrictPopulation? pop = dc.GetComponent<DistrictPopulation>();
                if (pop == null)
                    continue;

                if (_target == NeedCharacterTarget.Beaver)
                    foreach (Beaver beaver in pop.Beavers)
                        beaversAffected += TryApply(beaver, effect);
                else
                    foreach (Bot bot in pop.Bots)
                        botsAffected += TryApply(bot, effect);
            }

            Debug.Log($"[CycleReward] Need '{_needId}' delta {_amount:+0.##;-0.##;0} " +
                      $"applied to {beaversAffected} beaver(s), {botsAffected} bot(s).");
        }

        // Two typed overloads avoid the UnityEngine.Component ambiguity that
        // arises when Beaver/Bot are MonoBehaviours but the compiler can't see
        // their inheritance chain through the precompiled reference.
        private static int TryApply(Beaver beaver, in InstantEffect effect)
        {
            NeedManager? nm = beaver.GetComponent<NeedManager>();
            if (nm == null || !nm.HasNeed(effect.NeedId))
                return 0;
            nm.ApplyEffect(effect);
            return 1;
        }

        private static int TryApply(Bot bot, in InstantEffect effect)
        {
            NeedManager? nm = bot.GetComponent<NeedManager>();
            if (nm == null || !nm.HasNeed(effect.NeedId))
                return 0;
            nm.ApplyEffect(effect);
            return 1;
        }
    }
}
