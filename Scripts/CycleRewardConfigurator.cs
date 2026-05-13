using Bindito.Core;

namespace Agroqirax.Rewards
{
    [Context("Game")]
    public class CycleRewardConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<GoodSpecRepository>().AsSingleton();
            Bind<RewardPool>().AsSingleton();
            Bind<RewardSelectionPanel>().AsSingleton();
            Bind<CycleRewardService>().AsSingleton();
        }
    }
}
