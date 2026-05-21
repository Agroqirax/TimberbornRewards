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
        private readonly RewardPool            _rewardPool;
        private readonly RewardSelectionPanel  _selectionPanel;
        private readonly ILoc                  _loc;
        private readonly FactionService        _factionService;
        private readonly System.Random         _random = new();

        private bool _enabled;

        public CycleRewardService(
            EventBus              eventBus,
            RewardPool            rewardPool,
            RewardSelectionPanel  selectionPanel,
            ILoc                  loc,
            FactionService        factionService)
        {
            _eventBus       = eventBus;
            _rewardPool     = rewardPool;
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
            List<IReward> offered = DrawRewards(OfferedCount, e.Cycle);
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
        /// Candidates are first filtered to those eligible at <paramref name="cycle"/>
        /// (i.e. effective weight > 0 after all curve evaluations), so entries can be
        /// completely locked out at certain states via their curves.
        ///
        /// Each iteration does a weighted pick from the remaining candidates, removes
        /// the winner, then repeats — so the same reward can never appear twice
        /// regardless of its weight relative to others.
        /// </summary>
        private List<IReward> DrawRewards(int count, int cycle)
        {
            // Build context and evaluate all curves. Entries with weight <= 0 excluded.
            List<(IReward Reward, float Weight)> candidates =
                _rewardPool.GetWeightedForCycle(cycle);

            int drawCount = Math.Min(count, candidates.Count);
            var result    = new List<IReward>(drawCount);

            for (int i = 0; i < drawCount; i++)
            {
                double totalWeight = 0d;
                foreach (var (_, w) in candidates)
                    totalWeight += w;

                double roll    = _random.NextDouble() * totalWeight;
                double running = 0d;
                int    chosen  = candidates.Count - 1; // fallback to last

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
