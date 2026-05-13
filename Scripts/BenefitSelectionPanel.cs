using System;
using System.Collections.Generic;
using Timberborn.AssetSystem;
using Timberborn.CoreUI;
using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Displays the benefit selection dialog at cycle start.
    /// Follows the same pattern as <c>GameOptionsBox</c>: UXML is loaded fresh
    /// each cycle via <see cref="VisualElementLoader"/>, buttons are built in
    /// code and added to the container, then the panel is pushed onto the stack.
    ///
    /// We build buttons in code (using our local <see cref="NineSliceButton"/>)
    /// rather than declaring them in UXML because <c>cui:LocalizableButton</c>
    /// — the only public nine-slice button type — requires a <c>text-loc-key</c>
    /// attribute and throws during UXML initialisation without one.
    /// </summary>
    public class BenefitSelectionPanel : IPanelController
    {
        private static readonly CustomStyleProperty<Color> IconTintProperty =
            new CustomStyleProperty<Color>("--icon-tint");

        private static readonly string ViewPath    = "BenefitSelectionBox";
        private static readonly string TitleLocKey = "CycleBenefit.ChooseTitle";

        private readonly VisualElementLoader _visualElementLoader;
        private readonly IAssetLoader        _assetLoader;
        private readonly PanelStack          _panelStack;
        private readonly ILoc                _loc;

        private VisualElement?    _root;
        private Action<IBenefit>? _onChosen;

        public BenefitSelectionPanel(
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

        /// <summary>Populates the panel with the offered benefits and pushes it onto the stack.</summary>
        public void ShowFor(List<IBenefit> offered, Action<IBenefit> onChosen)
        {
            _onChosen = onChosen;

            _root = _visualElementLoader.LoadVisualElement(ViewPath);
            _root.Q<Label>("Title").text = _loc.T(TitleLocKey);

            VisualElement container = _root.Q<VisualElement>("BenefitButtons");
            foreach (IBenefit benefit in offered)
                container.Add(BuildButton(benefit));

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

        private VisualElement BuildButton(IBenefit benefit)
        {
            var button = new NineSliceButton();
            button.AddToClassList("menu-button");
            button.AddToClassList("menu-button--stretched");
            button.AddToClassList("benefit-button");
            button.RegisterCallback<ClickEvent>(_ => Choose(benefit));

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.flexGrow      = 1;
            button.Add(row);

            if (benefit.IconPath != null)
            {
                Sprite? sprite = _assetLoader.Load<Sprite>(benefit.IconPath);
                if (sprite != null)
                {
                    var icon = new Image();
                    icon.sprite    = sprite;
                    icon.scaleMode = ScaleMode.ScaleToFit;
                    icon.AddToClassList("benefit-button__icon");

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

            var label = new Label(benefit.GetDisplayName(_loc));
            label.AddToClassList("benefit-button__label");
            row.Add(label);

            return button;
        }

        private void Choose(IBenefit benefit)
        {
            _panelStack.Pop(this);
            _onChosen?.Invoke(benefit);
        }
    }
}