using System;
using System.Collections.Generic;
using Timberborn.GameCycleSystem;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Listens for the start of each cycle, draws 3 benefits from the pool,
    /// logs them to the Unity console / Player.log, and automatically applies
    /// the first one until a proper selection UI is added.
    /// </summary>
    public class CycleBenefitService : ILoadableSingleton
    {
        private const int OfferedCount = 3;
        private const string Tag = "[CycleBenefit]";

        private readonly EventBus _eventBus;
        private readonly BenefitPool _benefitPool;
        private readonly System.Random _random;

        public CycleBenefitService(EventBus eventBus, BenefitPool benefitPool)
        {
            _eventBus = eventBus;
            _benefitPool = benefitPool;
            _random = new System.Random();
        }

        // ILoadableSingleton.Load() is called after the game world finishes
        // loading. Register here so we don't miss events that fire during load,
        // exactly as GameCycleService does it.
        public void Load()
        {
            _eventBus.Register(this);
            Debug.Log($"{Tag} Registered with EventBus.");
        }

        [OnEvent]
        public void OnCycleStarted(CycleStartedEvent cycleStartedEvent)
        {
            // Skip cycle 1 (game start) — first reward arrives at cycle 2.
            // Remove this guard if you want a reward on the very first cycle.
            if (cycleStartedEvent.Cycle <= 1)
            {
                Debug.Log($"{Tag} Cycle {cycleStartedEvent.Cycle} started — skipping first cycle.");
                return;
            }

            Debug.Log($"{Tag} Cycle {cycleStartedEvent.Cycle} started — drawing benefits.");

            List<IBenefit> offered = DrawBenefits(OfferedCount);
            LogOfferedBenefits(cycleStartedEvent.Cycle, offered);

            // Auto-select index 0 until UI is implemented.
            IBenefit selected = offered[0];
            Debug.Log($"{Tag} Auto-selecting option 1: {selected.DisplayName}");
            selected.Apply();
            Debug.Log($"{Tag} Applied: {selected.DisplayName}");
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        /// <summary>
        /// Draws <paramref name="count"/> distinct entries from the pool using
        /// a partial Fisher-Yates shuffle so the same pool entry is never
        /// offered twice in one draw (even if duplicates exist in the pool).
        /// </summary>
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
            {
                Debug.Log($"{Tag}   Option {i + 1}: {offered[i].DisplayName}");
            }
        }
    }
}
