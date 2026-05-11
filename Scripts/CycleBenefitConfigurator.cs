using Bindito.Core;

namespace Agroqirax.Benefits
{
    [Context("Game")]
    public class CycleBenefitConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<GoodSpecRepository>().AsSingleton();
            Bind<BenefitPool>().AsSingleton();
            Bind<BenefitSelectionPanel>().AsSingleton();
            Bind<CycleBenefitService>().AsSingleton();
            // FactionService and DistrictCenterRegistry are game singletons —
            // Bindito resolves them automatically via injection.
        }
    }
}
