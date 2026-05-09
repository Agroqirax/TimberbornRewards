using Bindito.Core;

namespace Agroqirax.Benefits
{
    [Context("Game")]
    public class CycleBenefitConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<BenefitPool>().AsSingleton();
            Bind<BenefitSelectionPanel>().AsSingleton();
            Bind<CycleBenefitService>().AsSingleton();
            // DistrictCenterRegistry is already bound by the game — Bindito
            // resolves it automatically when injecting into BenefitPool.
        }
    }
}