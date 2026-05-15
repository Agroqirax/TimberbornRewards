#nullable enable
using Timberborn.Localization;
using Timberborn.ScienceSystem;

namespace Agroqirax.Rewards
{
    public class SciencePointReward : IReward
    {
        private static readonly string SingularLocKey = "CycleReward.SciencePoints.DisplayName";
        private static readonly string PluralLocKey   = "CycleReward.SciencePoints.PluralDisplayName";
        private static readonly string IconPath_      = "sprites/topbar/Science";

        private readonly ScienceService _scienceService;
        private readonly int            _amount;

        public string? IconPath => IconPath_;

        public SciencePointReward(ScienceService scienceService, int amount)
        {
            _scienceService = scienceService;
            _amount         = amount;
        }

        public string GetDisplayName(ILoc loc)
        {
            string signed  = _amount > 0 ? $"+{_amount}" : _amount.ToString();
            string locKey  = _amount == 1 ? SingularLocKey : PluralLocKey;
            return loc.T(locKey, signed);
        }

        public void Apply() => _scienceService.AddPoints(_amount);
    }
}