using Timberborn.BlueprintSystem;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// A minimal mirror of the game's built-in GoodSpec, containing only the
    /// fields we actually need.  Deserialised by ISpecService from each
    /// GoodSpec.*.blueprint.json that the game / other mods ship.
    /// </summary>
    public record GoodSpecRecord : ComponentSpec
    {
        [Serialize] public string Id                  { get; init; } = "";
        [Serialize] public string DisplayNameLocKey   { get; init; } = "";
        [Serialize] public string PluralDisplayNameLocKey { get; init; } = "";
        /// <summary>
        /// Asset path for the good's icon, e.g. "Sprites/Goods/CarrotIcon".
        /// Stored in GoodSpec as "Icon".
        /// </summary>
        [Serialize] public string Icon                { get; init; } = "";
    }
}
