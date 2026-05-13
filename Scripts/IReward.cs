using Timberborn.Localization;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A single selectable reward offered to the player at cycle start.
    /// </summary>
    public interface IReward
    {
        /// <summary>Returns the fully resolved display name.</summary>
        string GetDisplayName(ILoc loc);

        /// <summary>
        /// Asset path for the icon, relative to the Resources root.
        /// e.g. "Sprites/Goods/CarrotIcon" — no extension. Null = no icon.
        /// </summary>
        string? IconPath { get; }

        /// <summary>Applies this reward to the game world.</summary>
        void Apply();
    }
}
