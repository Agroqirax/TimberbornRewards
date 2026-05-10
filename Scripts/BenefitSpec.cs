using System.Collections.Immutable;
using Timberborn.BlueprintSystem;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Singleton spec loaded from Configurations/Benefits.blueprint.json.
    /// Holds the full list of benefit entries; deserialized via ISpecService.
    /// </summary>
    public record BenefitSpec : ComponentSpec
    {
        [Serialize]
        public ImmutableArray<BenefitEntrySpec> Benefits { get; init; }
    }
}
