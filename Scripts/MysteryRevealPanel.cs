#nullable enable
using System;
using Timberborn.AssetSystem;
using Timberborn.CoreUI;
using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agroqirax.Rewards
{
    public class MysteryRevealPanel : IPanelController
    {
        private static readonly string ViewPath    = "MysteryRevealBox";
        private static readonly string TitleLocKey = "CycleReward.Mystery.RevealTitle";

        private readonly VisualElementLoader _visualElementLoader;
        private readonly IAssetLoader        _assetLoader;
        private readonly PanelStack          _panelStack;
        private readonly ILoc                _loc;

        private VisualElement? _root;
        private Action?        _onDismissed;

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

        public void ShowFor(IReward reward, Action onDismissed)
        {
            _onDismissed = onDismissed;
            _root = _visualElementLoader.LoadVisualElement(ViewPath);

            _root.Q<Label>("RevealTitle").text      = _loc.T(TitleLocKey);
            _root.Q<Label>("RevealRewardName").text = reward.GetDisplayName(_loc);

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

            _panelStack.HideAndPushDialog(this);
        }

        // ---------------------------------------------------------------
        // IPanelController
        // ---------------------------------------------------------------

        public VisualElement GetPanel() => _root!;

        public bool OnUIConfirmed()
        {
            Dismiss();
            return true;
        }

        public void OnUICancelled() { } // not cancellable

        // ---------------------------------------------------------------

        private void Dismiss()
        {
            _panelStack.Pop(this);   // pop reveal — selection panel is now on top again
            _onDismissed?.Invoke();  // pop selection panel
        }
    }
}