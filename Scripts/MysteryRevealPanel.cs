#nullable enable
using Timberborn.AssetSystem;
using Timberborn.CoreUI;
using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// A one-shot dialog pushed onto the panel stack after the player picks the
    /// mystery reward. Shows the real reward's icon, its resolved display name,
    /// and an OK button to dismiss.
    ///
    /// <para>
    /// Follows the same load-fresh-UXML-per-show pattern as
    /// <see cref="RewardSelectionPanel"/>. The reward has already been applied
    /// by the time this panel is shown — it is purely informational.
    /// </para>
    /// </summary>
    public class MysteryRevealPanel : IPanelController
    {
        private static readonly string ViewPath    = "MysteryRevealBox";
        private static readonly string TitleLocKey = "CycleReward.Mystery.RevealTitle";

        private readonly VisualElementLoader _visualElementLoader;
        private readonly IAssetLoader        _assetLoader;
        private readonly PanelStack          _panelStack;
        private readonly ILoc                _loc;

        private VisualElement? _root;

        public MysteryRevealPanel(
            VisualElementLoader visualElementLoader,
            IAssetLoader        assetLoader,
            PanelStack          panelStack,
            ILoc                loc)
        {
            _visualElementLoader = visualElementLoader;
            _assetLoader         = assetLoader;
            _panelStack          = panelStack;
            _loc                 = loc;
        }

        /// <summary>
        /// Builds the reveal panel for <paramref name="reward"/> and pushes it
        /// onto the stack. The reward must already have been applied before
        /// calling this.
        /// </summary>
        public void ShowFor(IReward reward)
        {
            _root = _visualElementLoader.LoadVisualElement(ViewPath);

            _root.Q<Label>("RevealTitle").text = _loc.T(TitleLocKey);
            _root.Q<Label>("RevealRewardName").text = reward.GetDisplayName(_loc);

            // Icon — use the reward's own icon if available, fall back to nothing.
            var iconElement = _root.Q<Image>("RevealIcon");
            if (reward.IconPath != null)
            {
                Sprite? sprite = _assetLoader.Load<Sprite>(reward.IconPath);
                if (sprite != null)
                {
                    iconElement.sprite    = sprite;
                    iconElement.scaleMode = ScaleMode.ScaleToFit;
                }
                else
                {
                    iconElement.style.display = DisplayStyle.None;
                }
            }
            else
            {
                iconElement.style.display = DisplayStyle.None;
            }

            _root.Q<Button>("OkButton").RegisterCallback<ClickEvent>(_ => Dismiss());

            _panelStack.PushDialog(this);
        }

        // ---------------------------------------------------------------
        // IPanelController
        // ---------------------------------------------------------------

        public VisualElement GetPanel() => _root!;

        /// <summary>OK button / confirm key both close the panel.</summary>
        public bool OnUIConfirmed()
        {
            Dismiss();
            return true;
        }

        public void OnUICancelled() { } // not cancellable

        // ---------------------------------------------------------------

        private void Dismiss() => _panelStack.Pop(this);
    }
}