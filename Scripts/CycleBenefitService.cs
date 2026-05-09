using System;
using System.Collections.Generic;
using Timberborn.GameCycleSystem;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Listens for the start of each cycle, draws 3 benefits from the pool,
    /// and opens the selection panel for the player to choose one.
    /// </summary>
    public class CycleBenefitService : ILoadableSingleton
    {
        private const int OfferedCount = 3;
        private const string Tag = "[CycleBenefit]";

        private readonly EventBus _eventBus;
        private readonly BenefitPool _benefitPool;
        private readonly BenefitSelectionPanel _selectionPanel;
        private readonly System.Random _random;

        public CycleBenefitService(EventBus eventBus,
                                   BenefitPool benefitPool,
                                   BenefitSelectionPanel selectionPanel)
        {
            _eventBus      = eventBus;
            _benefitPool   = benefitPool;
            _selectionPanel = selectionPanel;
            _random        = new System.Random();
        }

        public void Load()
        {
            _eventBus.Register(this);
            Debug.Log($"{Tag} Registered with EventBus.");
        }

        [OnEvent]
        public void OnCycleStarted(CycleStartedEvent cycleStartedEvent)
        {
            if (cycleStartedEvent.Cycle <= 1)
            {
                Debug.Log($"{Tag} Cycle {cycleStartedEvent.Cycle} — skipping first cycle.");
                return;
            }

            Debug.Log($"{Tag} Cycle {cycleStartedEvent.Cycle} started — offering benefits.");

            List<IBenefit> offered = DrawBenefits(OfferedCount);
            LogOfferedBenefits(cycleStartedEvent.Cycle, offered);

            _selectionPanel.ShowFor(offered, OnBenefitChosen);
        }

        private void OnBenefitChosen(IBenefit benefit)
        {
            Debug.Log($"{Tag} Applying: {benefit.DisplayName}");
            benefit.Apply();
            Debug.Log($"{Tag} Done.");
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private List<IBenefit> DrawBenefits(int count)
        {
            IReadOnlyList<IBenefit> pool = _benefitPool.All;
            int drawCount = Math.Min(count, pool.Count);

            var indices = new List<int>(pool.Count);
            for (int i = 0; i < pool.Count; i++) indices.Add(i);

            var result = new List<IBenefit>(drawCount);
            for (int i = 0; i < drawCount; i++)
            {
                int j = i + _random.Next(pool.Count - i);
                (indices[i], indices[j]) = (indices[j], indices[i]);
                result.Add(pool[indices[i]]);
            }

            return result;
        }

        private static void LogOfferedBenefits(int cycle, List<IBenefit> offered)
        {
            Debug.Log($"{Tag} === Cycle {cycle} — Choose a benefit ===");
            for (int i = 0; i < offered.Count; i++)
                Debug.Log($"{Tag}   Option {i + 1}: {offered[i].DisplayName}");
        }
    }
}
