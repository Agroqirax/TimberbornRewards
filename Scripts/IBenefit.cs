namespace Agroqirax.Benefits
{
    /// <summary>
    /// Represents a single selectable benefit that can be offered to the player at cycle start.
    /// </summary>
    public interface IBenefit
    {
        /// <summary>Short display name shown when listing options, e.g. "+50 Science Points".</summary>
        string DisplayName { get; }

        /// <summary>Apply this benefit to the game world.</summary>
        void Apply();
    }
}
