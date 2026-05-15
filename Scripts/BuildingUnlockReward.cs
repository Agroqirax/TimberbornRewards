#nullable enable
using Timberborn.BlockObjectTools;
using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using Timberborn.ScienceSystem;
using Timberborn.TemplateSystem;
using Timberborn.ToolButtonSystem;
using Timberborn.ToolSystem;
using UnityEngine;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A reward that immediately unlocks a building for the player at no science cost.
    ///
    /// <para>
    /// Uses <see cref="BuildingUnlockingService.UnlockIgnoringCost"/> so the unlock
    /// fires the normal <c>BuildingUnlockedEvent</c> and persists to the save file,
    /// but does not deduct science points or show a confirmation dialog.
    /// </para>
    ///
    /// <para>
    /// Already-unlocked buildings are silently re-unlocked (idempotent); the service
    /// simply re-adds the template name to its <c>HashSet</c>, which is harmless.
    /// The pool builder in <see cref="RewardPool"/> should filter these out at draw
    /// time using <see cref="BuildingUnlockingService.Unlocked"/> so they never appear
    /// as choices once the player already has them.
    /// </para>
    /// </summary>
    /// <summary>
    /// A reward that immediately unlocks a building for the player at no science cost.
    ///
    /// <para>
    /// Two things must happen for an unlock to fully take effect in the live session:
    /// <list type="number">
    ///   <item>
    ///     <see cref="BuildingUnlockingService.UnlockIgnoringCost"/> — persists the
    ///     unlock to the save data and fires <c>BuildingUnlockedEvent</c>, but does
    ///     not touch the tool layer.
    ///   </item>
    ///   <item>
    ///     <see cref="ToolUnlockingService.Unlock"/> on the matching
    ///     <see cref="BlockObjectTool"/> — removes it from
    ///     <c>ToolUnlockingService._activeLockers</c> and fires
    ///     <c>ToolUnlockedEvent</c>, which is what the UI actually listens to in
    ///     order to re-enable the toolbar button. Without this step the button stays
    ///     greyed out until the next save/load cycle.
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    public class BuildingUnlockReward : IReward
    {
        private static readonly string LocKey = "CycleReward.BuildingUnlock";

        private readonly BuildingUnlockingService _buildingUnlockingService;
        private readonly ToolUnlockingService     _toolUnlockingService;
        private readonly ToolButtonService        _toolButtonService;
        private readonly BuildingSpec             _buildingSpec;
        private readonly string                   _displayNameLocKey;
        private readonly string                   _templateName;
        private readonly string?                  _iconPath;

        public string? IconPath => _iconPath;

        public BuildingUnlockReward(
            BuildingUnlockingService buildingUnlockingService,
            ToolUnlockingService     toolUnlockingService,
            ToolButtonService        toolButtonService,
            BuildingSpec             buildingSpec,
            string                   displayNameLocKey,
            string                   templateName,
            string?                  iconPath)
        {
            _buildingUnlockingService = buildingUnlockingService;
            _toolUnlockingService     = toolUnlockingService;
            _toolButtonService        = toolButtonService;
            _buildingSpec             = buildingSpec;
            _displayNameLocKey        = displayNameLocKey;
            _templateName             = templateName;
            _iconPath                 = iconPath;
        }

        public string GetDisplayName(ILoc loc)
            => loc.T(LocKey, loc.T(_displayNameLocKey));

        public void Apply()
        {
            // Step 1: persist the unlock and fire BuildingUnlockedEvent.
            _buildingUnlockingService.UnlockIgnoringCost(_buildingSpec);

            // Step 2: find every BlockObjectTool whose template matches this building
            // and remove it from ToolUnlockingService._activeLockers so the toolbar
            // button becomes live immediately without requiring a save/reload.
            int unlocked = 0;
            foreach (ToolButton toolButton in _toolButtonService.ToolButtons)
            {
                if (toolButton.Tool is not BlockObjectTool blockObjectTool)
                    continue;

                TemplateSpec? templateSpec = blockObjectTool.Template.GetSpec<TemplateSpec>();
                if (templateSpec == null || !templateSpec.IsNamedExactly(_templateName))
                    continue;

                if (_toolUnlockingService.IsLocked(blockObjectTool))
                {
                    _toolUnlockingService.Unlock(blockObjectTool);
                    unlocked++;
                }
            }

            Debug.Log($"[CycleReward] Unlocked building '{_templateName}' " +
                      $"(tool entries unlocked: {unlocked}).");
        }
    }
}
