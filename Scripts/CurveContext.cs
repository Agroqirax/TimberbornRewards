namespace Agroqirax.Rewards
{
    /// <summary>
    /// All runtime values needed to evaluate any curve type at reward-draw time.
    /// Built once per draw call in <see cref="RewardPool"/> and passed to
    /// <see cref="RewardEntrySpec.GetWeightAt"/>.
    /// </summary>
    public readonly struct CurveContext
    {
        /// <summary>Current game cycle number.</summary>
        public readonly int Cycle;

        /// <summary>Current science point total.</summary>
        public readonly int SciencePoints;

        /// <summary>
        /// Resolver for global good amounts. Call <see cref="GoodAmountResolver.Get"/>
        /// with a good ID to get its current global available stock.
        /// Lazily evaluated — goods not referenced by any curve are never queried.
        /// </summary>
        public readonly GoodAmountResolver Goods;

        /// <summary>
        /// Resolver for global average need points (× 100). Call
        /// <see cref="NeedAverageResolver.Get"/> with a need ID to get the average.
        /// Lazily evaluated per need ID.
        /// </summary>
        public readonly NeedAverageResolver Needs;

        /// <summary>
        /// Resolver for global population counts by <see cref="PopulationCurveTarget"/>.
        /// Lazily evaluated per target type.
        /// </summary>
        public readonly PopulationCountResolver Population;

        public CurveContext(
            int                    cycle,
            int                    sciencePoints,
            GoodAmountResolver     goods,
            NeedAverageResolver    needs,
            PopulationCountResolver population)
        {
            Cycle         = cycle;
            SciencePoints = sciencePoints;
            Goods         = goods;
            Needs         = needs;
            Population    = population;
        }
    }
}
