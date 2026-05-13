using Timberborn.Localization;
using Timberborn.ScienceSystem;

namespace Agroqirax.Benefits
{
    public class SciencePointBenefit : IBenefit
    {
        private static readonly string LocKey   = "CycleBenefit.SciencePoints";
        private static readonly string IconPath_ = "sprites/topbar/Science";

        private readonly ScienceService _scienceService;
        private readonly int            _amount;

        public string? IconPath => IconPath_;

        public SciencePointBenefit(ScienceService scienceService, int amount)
        {
            _scienceService = scienceService;
            _amount         = amount;
        }

        public string GetDisplayName(ILoc loc) => loc.T(LocKey, _amount);

        public void Apply() => _scienceService.AddPoints(_amount);
    }
}
