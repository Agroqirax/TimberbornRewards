using System;
using System.Collections.Generic;
using Timberborn.GameCycleSystem;
using Timberborn.GameFactionSystem;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace Agroqirax.Rewards
{
    public class CycleRewardService : ILoadableSingleton
    {
        private const int    OfferedCount = 3;
        private const string Tag          = "[CycleReward]";

        private readonly EventBus              _eventBus;
        private readonly RewardPool           _rewardPool;
        private readonly RewardSelectionPanel _selectionPanel;
        private readonly ILoc                  _loc;
        private readonly FactionService        _factionService;
        private readonly System.Random         _random = new();

        private bool _enabled;

        public CycleRewardService(
            EventBus              eventBus,
            RewardPool           rewardPool,
            RewardSelectionPanel selectionPanel,
            ILoc                  loc,
            FactionService        factionService)
        {
            _eventBus       = eventBus;
            _rewardPool    = rewardPool;
            _selectionPanel = selectionPanel;
            _loc            = loc;
            _factionService = factionService;
        }

        public void Load()
        {
            string factionId = _factionService.Current.Id;
            _enabled = _rewardPool.InitForFaction(factionId);

            if (_enabled)
            {
                _eventBus.Register(this);
                Debug.Log($"{Tag} Enabled for faction '{factionId}'.");
            }
            else
            {
                Debug.LogWarning($"{Tag} Disabled — no reward spec found for faction '{factionId}'.");
            }
        }

        [OnEvent]
        public void OnCycleStarted(CycleStartedEvent e)
        {
            // Cycle 1 fires on initial game load; the first reward offer is on cycle 2.
            if (e.Cycle <= 1)
                return;

            Debug.Log($"{Tag} Cycle {e.Cycle} — offering rewards.");
            List<IReward> offered = DrawRewards(OfferedCount);
            _selectionPanel.ShowFor(offered, OnRewardChosen);
        }

        private void OnRewardChosen(IReward reward)
        {
            Debug.Log($"{Tag} Applying '{reward.GetDisplayName(_loc)}'.");
            reward.Apply();
        }

        /// <summary>
        /// Draws <paramref name="count"/> distinct rewards using weighted random
        /// sampling without replacement.
        ///
        /// Each call does a weighted pick from the remaining candidates, removes
        /// the winner, then repeats — so the same reward can never appear twice
        /// regardless of its weight relative to others.
        /// </summary>
        private List<IReward> DrawRewards(int count)
        {
            // Copy the unique weighted entries so we can remove winners without
            // mutating the pool itself.
            List<(IReward Reward, int Weight)> candidates =
                new List<(IReward, int)>(_rewardPool.UniqueWeighted);

            int drawCount = Math.Min(count, candidates.Count);
            var result    = new List<IReward>(drawCount);

            for (int i = 0; i < drawCount; i++)
            {
                int totalWeight = 0;
                foreach (var (_, w) in candidates)
                    totalWeight += w;

                int roll    = _random.Next(totalWeight);
                int running = 0;
                int chosen  = candidates.Count - 1; // fallback to last

                for (int j = 0; j < candidates.Count; j++)
                {
                    running += candidates[j].Weight;
                    if (roll < running)
                    {
                        chosen = j;
                        break;
                    }
                }

                result.Add(candidates[chosen].Reward);
                candidates.RemoveAt(chosen);
            }

            return result;
        }
    }
}