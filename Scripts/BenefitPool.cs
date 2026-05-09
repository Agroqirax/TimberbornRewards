using System.Collections.Generic;
using Timberborn.GameDistricts;
using Timberborn.ScienceSystem;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Holds the full pool of benefits that can be offered each cycle.
    /// Duplicates act as weights — two copies = twice the draw probability.
    /// </summary>
    public class BenefitPool
    {
        private readonly ScienceService _scienceService;
        private readonly DistrictCenterRegistry _districtCenterRegistry;

        private List<IBenefit>? _pool;

        public BenefitPool(ScienceService scienceService,
                           DistrictCenterRegistry districtCenterRegistry)
        {
            _scienceService          = scienceService;
            _districtCenterRegistry  = districtCenterRegistry;
        }

        public IReadOnlyList<IBenefit> All => _pool ??= BuildPool();

        private List<IBenefit> BuildPool()
        {
            return new List<IBenefit>
            {
                // ----- Science points -----
                Science(50),
                Science(50),
                Science(150),
                Science(150),
                Science(300),
                Science(500),

                // ----- Resources -----
                // Logs — common, lower amounts
                Resource("Log",       30,  "sprites/goods/LogIcon"),
                Resource("Log",       60,  "sprites/goods/LogIcon"),
                // Planks
                Resource("Plank",     20,  "sprites/goods/PlankIcon"),
                Resource("Plank",     40,  "sprites/goods/PlankIcon"),
                // Food
                Resource("Carrot",    40,  "sprites/goods/CarrotIcon"),
                Resource("Blueberry", 40,  "sprites/goods/BlueberryIcon"),
                // Gear
                Resource("Gear",      10,  "sprites/goods/GearIcon"),
                Resource("Gear",      20,  "sprites/goods/GearIcon"),
                // Paper
                Resource("Paper",     15,  "sprites/goods/PaperIcon"),
                Resource("Paper",     30,  "sprites/goods/PaperIcon"),
            };
        }

        // ---------------------------------------------------------------
        // Convenience factory methods — keep BuildPool readable
        // ---------------------------------------------------------------

        private IBenefit Science(int amount)
            => new SciencePointBenefit(_scienceService, amount);

        private IBenefit Resource(string goodId, int amount, string iconPath)
            => new ResourceBenefit(_districtCenterRegistry, goodId, amount, iconPath);
    }
}
