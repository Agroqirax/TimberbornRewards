using Timberborn.ScienceSystem;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// A benefit that grants the player a fixed number of science points.
    /// </summary>
    public class SciencePointBenefit : IBenefit
    {
        private readonly ScienceService _scienceService;
        private readonly int _amount;

        public string DisplayName => $"+{_amount} Science Points";

        public SciencePointBenefit(ScienceService scienceService, int amount)
        {
            _scienceService = scienceService;
            _amount = amount;
        }

        public void Apply()
        {
            _scienceService.AddPoints(_amount);
        }
    }
}
