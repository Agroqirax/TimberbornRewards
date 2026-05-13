using Timberborn.Localization;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// A single selectable benefit offered to the player at cycle start.
    /// </summary>
    public interface IBenefit
    {
        /// <summary>Returns the fully resolved display name.</summary>
        string GetDisplayName(ILoc loc);

        /// <summary>
        /// Asset path for the icon, relative to the Resources root.
        /// e.g. "Sprites/Goods/CarrotIcon" — no extension. Null = no icon.
        /// </summary>
        string? IconPath { get; }

        /// <summary>Applies this benefit to the game world.</summary>
        void Apply();
    }
}
