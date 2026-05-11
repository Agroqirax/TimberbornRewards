using System.Collections.Immutable;
using Timberborn.BlueprintSystem;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Per-faction spec loaded from e.g.
    ///   Configurations/Benefits.Folktails.blueprint.json
    ///   Configurations/Benefits.IronTeeth.blueprint.json
    ///
    /// The <see cref="FactionId"/> must match the faction's Id as returned by
    /// <c>FactionService.CurrentFaction.Id</c> (e.g. "Folktails", "IronTeeth").
    ///
    /// Custom-faction mod authors can ship their own blueprint alongside this
    /// mod to add support for their faction without touching this mod's code.
    /// </summary>
    public record FactionBenefitSpec : ComponentSpec
    {
        [Serialize] public string                          FactionId { get; init; } = "";
        [Serialize] public ImmutableArray<BenefitEntrySpec> Benefits  { get; init; }
    }
}
