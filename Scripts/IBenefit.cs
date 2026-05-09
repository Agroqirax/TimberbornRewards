using Timberborn.Localization;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Represents a single selectable benefit offered to the player at cycle start.
    /// </summary>
    public interface IBenefit
    {
        /// <summary>
        /// Returns the fully resolved display name using the provided localization
        /// service. Implementations can use parameters e.g. loc.T(key, amount).
        /// </summary>
        string GetDisplayName(ILoc loc);

        /// <summary>
        /// Asset path for the icon, relative to the Resources root.
        /// e.g. "sprites/topbar/Science" — no extension.
        /// Null = no icon shown.
        /// </summary>
        string? IconPath { get; }

        /// <summary>Apply this benefit to the game world.</summary>
        void Apply();
    }
}
