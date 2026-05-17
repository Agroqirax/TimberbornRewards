#nullable enable
using System;
using System.Collections.Generic;
using Timberborn.AssetSystem;
using Timberborn.CoreUI;
using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agroqirax.Rewards
{
    /// <summary>
    /// Displays the reward selection dialog at cycle start.
    /// Follows the same pattern as <c>GameOptionsBox</c>: UXML is loaded fresh
    /// each cycle via <see cref="VisualElementLoader"/>, buttons are built in
    /// code and added to the container, then the panel is pushed onto the stack.
    ///
    /// We build buttons in code (using our local <see cref="NineSliceButton"/>)
    /// rather than declaring them in UXML because <c>cui:LocalizableButton</c>
    /// — the only public nine-slice button type — requires a <c>text-loc-key</c>
    /// attribute and throws during UXML initialisation without one.
    ///
    /// The last offered reward is always presented as a "mystery" option: its
    /// icon and label are hidden behind a question mark and a generic string.
    /// When the player picks it the reward is applied and then
    /// <see cref="MysteryRevealPanel"/> is pushed to show what they received.
    /// </summary>
    public class RewardSelectionPanel : IPanelController
    {
        private static readonly CustomStyleProperty<Color> IconTintProperty =
            new CustomStyleProperty<Color>("--icon-tint");

        private static readonly string ViewPath        = "RewardSelectionBox";
        private static readonly string TitleLocKey     = "CycleReward.ChooseTitle";
        private static readonly string MysteryLocKey   = "CycleReward.Mystery.DisplayName";
        private static readonly string MysteryIconPath = "ui/images/buttons/question-mark";

        private readonly VisualElementLoader _visualElementLoader;
        private readonly IAssetLoader        _assetLoader;
        private readonly PanelStack          _panelStack;
        private readonly ILoc                _loc;
        private readonly MysteryRevealPanel  _mysteryRevealPanel;

        private VisualElement?    _root;
        private Action<IReward>? _onChosen;

        public RewardSelectionPanel(
            VisualElementLoader visualElementLoader,
            IAssetLoader        assetLoader,
            PanelStack          panelStack,
            ILoc                loc,
            MysteryRevealPanel  mysteryRevealPanel)
        {
            _visualElementLoader = visualElementLoader;
            _assetLoader         = assetLoader;
            _panelStack          = panelStack;
            _loc                 = loc;
            _mysteryRevealPanel  = mysteryRevealPanel;
        }

        /// <summary>Populates the panel with the offered rewards and pushes it onto the stack.</summary>
        public void ShowFor(List<IReward> offered, Action<IReward> onChosen)
        {
            _onChosen = onChosen;

            _root = _visualElementLoader.LoadVisualElement(ViewPath);
            _root.Q<Label>("Title").text = _loc.T(TitleLocKey);

            VisualElement container = _root.Q<VisualElement>("RewardButtons");
            for (int i = 0; i < offered.Count; i++)
            {
                bool isMystery = (i == offered.Count - 1) && offered.Count > 1;
                container.Add(isMystery
                    ? BuildMysteryButton(offered[i])
                    : BuildButton(offered[i]));
            }

            _panelStack.PushDialog(this);
        }

        // ---------------------------------------------------------------
        // IPanelController
        // ---------------------------------------------------------------

        public VisualElement GetPanel() => _root!;

        public bool OnUIConfirmed() => false;  // no default — player must click

        public void OnUICancelled() { }        // intentionally non-cancellable

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Builds a normal reward button whose icon and label are fully visible.
        /// </summary>
        private VisualElement BuildButton(IReward reward)
        {
            var button = new NineSliceButton();
            button.AddToClassList("menu-button");
            button.AddToClassList("menu-button--stretched");
            button.AddToClassList("reward-button");
            button.RegisterCallback<ClickEvent>(_ => Choose(reward));

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.flexGrow      = 1;
            button.Add(row);

            if (reward.IconPath != null)
            {
                Sprite? sprite = _assetLoader.Load<Sprite>(reward.IconPath);
                if (sprite != null)
                {
                    var icon = new Image();
                    icon.sprite    = sprite;
                    icon.scaleMode = ScaleMode.ScaleToFit;
                    icon.AddToClassList("reward-button__icon");

                    // Read --icon-tint from USS so hover colour is driven declaratively
                    // rather than via MouseEnter/Leave callbacks.
                    icon.RegisterCallback<CustomStyleResolvedEvent>(_ =>
                    {
                        if (icon.customStyle.TryGetValue(IconTintProperty, out Color tint))
                            icon.style.unityBackgroundImageTintColor = new StyleColor(tint);
                    });

                    row.Add(icon);
                }
            }

            var label = new Label(reward.GetDisplayName(_loc));
            label.AddToClassList("reward-button__label");
            row.Add(label);

            return button;
        }

        /// <summary>
        /// Builds a mystery reward button. The question-mark icon and a generic
        /// "???" label are shown; the real reward is only revealed on click.
        /// </summary>
        private VisualElement BuildMysteryButton(IReward reward)
        {
            var button = new NineSliceButton();
            button.AddToClassList("menu-button");
            button.AddToClassList("menu-button--stretched");
            button.AddToClassList("reward-button");
            button.AddToClassList("reward-button--mystery");
            button.RegisterCallback<ClickEvent>(_ => ChooseMystery(reward));

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.flexGrow      = 1;
            button.Add(row);

            // Always show the question-mark icon regardless of the real reward's icon.
            Sprite? questionSprite = _assetLoader.Load<Sprite>(MysteryIconPath);
            if (questionSprite != null)
            {
                var icon = new Image();
                icon.sprite    = questionSprite;
                icon.scaleMode = ScaleMode.ScaleToFit;
                icon.AddToClassList("reward-button__icon");
                icon.AddToClassList("reward-button__icon--mystery");

                icon.RegisterCallback<CustomStyleResolvedEvent>(_ =>
                {
                    if (icon.customStyle.TryGetValue(IconTintProperty, out Color tint))
                        icon.style.unityBackgroundImageTintColor = new StyleColor(tint);
                });

                row.Add(icon);
            }

            var label = new Label(_loc.T(MysteryLocKey));
            label.AddToClassList("reward-button__label");
            label.AddToClassList("reward-button__label--mystery");
            row.Add(label);

            return button;
        }

        private void Choose(IReward reward)
        {
            _panelStack.Pop(this);
            _onChosen?.Invoke(reward);
        }

        /// <summary>
        /// Called when the player picks the mystery reward. The selection panel
        /// is popped first, then the reward is applied via <see cref="_onChosen"/>,
        /// and finally the reveal panel is pushed so the player sees what they got.
        /// </summary>
        private void ChooseMystery(IReward reward)
        {
            _onChosen?.Invoke(reward);
            _mysteryRevealPanel.ShowFor(reward, () => _panelStack.Pop(this));
        }
    }
}
