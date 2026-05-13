using System;
using System.Collections.Generic;
using Timberborn.GameCycleSystem;
using Timberborn.GameFactionSystem;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace Agroqirax.Benefits
{
    public class CycleBenefitService : ILoadableSingleton
    {
        private const int    OfferedCount = 3;
        private const string Tag          = "[CycleBenefit]";

        private readonly EventBus              _eventBus;
        private readonly BenefitPool           _benefitPool;
        private readonly BenefitSelectionPanel _selectionPanel;
        private readonly ILoc                  _loc;
        private readonly FactionService        _factionService;
        private readonly System.Random         _random = new();

        private bool _enabled;

        public CycleBenefitService(
            EventBus              eventBus,
            BenefitPool           benefitPool,
            BenefitSelectionPanel selectionPanel,
            ILoc                  loc,
            FactionService        factionService)
        {
            _eventBus       = eventBus;
            _benefitPool    = benefitPool;
            _selectionPanel = selectionPanel;
            _loc            = loc;
            _factionService = factionService;
        }

        public void Load()
        {
            string factionId = _factionService.Current.Id;
            _enabled = _benefitPool.InitForFaction(factionId);

            if (_enabled)
            {
                _eventBus.Register(this);
                Debug.Log($"{Tag} Enabled for faction '{factionId}'.");
            }
            else
            {
                Debug.LogWarning($"{Tag} Disabled — no benefit spec found for faction '{factionId}'.");
            }
        }

        [OnEvent]
        public void OnCycleStarted(CycleStartedEvent e)
        {
            // Cycle 1 fires on initial game load; the first benefit offer is on cycle 2.
            if (e.Cycle <= 1)
                return;

            Debug.Log($"{Tag} Cycle {e.Cycle} — offering benefits.");
            List<IBenefit> offered = DrawBenefits(OfferedCount);
            _selectionPanel.ShowFor(offered, OnBenefitChosen);
        }

        private void OnBenefitChosen(IBenefit benefit)
        {
            Debug.Log($"{Tag} Applying '{benefit.GetDisplayName(_loc)}'.");
            benefit.Apply();
        }

        /// <summary>
        /// Draws <paramref name="count"/> distinct benefits from the weighted pool
        /// using a partial Fisher-Yates shuffle on an index list.
        /// </summary>
        private List<IBenefit> DrawBenefits(int count)
        {
            IReadOnlyList<IBenefit> pool      = _benefitPool.All;
            int                     drawCount = Math.Min(count, pool.Count);

            var indices = new List<int>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
                indices.Add(i);

            var result = new List<IBenefit>(drawCount);
            for (int i = 0; i < drawCount; i++)
            {
                int j = _random.Next(i, pool.Count);
                (indices[i], indices[j]) = (indices[j], indices[i]);
                result.Add(pool[indices[i]]);
            }
            return result;
        }
    }
}
