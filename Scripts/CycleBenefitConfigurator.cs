using Bindito.Core;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Bindito configurator for the CycleBenefitMod.
    /// [Context("Game")] means it only activates during gameplay, not in the
    /// map editor — matching the pattern from the game's own configurators.
    /// Unity/the mod builder finds this class automatically by scanning the
    /// assembly; no plugin entry point is needed.
    /// </summary>
    [Context("Game")]
    public class CycleBenefitConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<BenefitPool>().AsSingleton();
            Bind<CycleBenefitService>().AsSingleton();
        }
    }
}
