using System.Collections.Immutable;
using Timberborn.BlueprintSystem;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Per-faction benefit configuration, loaded from
    /// <c>Configurations/Benefits.{FactionId}.blueprint.json</c>.
    ///
    /// The <see cref="FactionId"/> must match the value returned by
    /// <c>FactionService.Current.Id</c> (e.g. "Folktails", "IronTeeth").
    ///
    /// Custom-faction mod authors can ship their own blueprint alongside this
    /// mod to add support for their faction without touching this mod's code.
    /// </summary>
    public record FactionBenefitSpec : ComponentSpec
    {
        [Serialize] public string                             FactionId { get; init; } = "";
        [Serialize] public ImmutableArray<BenefitEntrySpec>  Benefits  { get; init; }
    }
}
