using System;
using System.Collections.Generic;
using Timberborn.AssetSystem;
using Timberborn.CoreUI;
using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agroqirax.Benefits
{
    public class BenefitSelectionPanel : IPanelController
    {
        private static readonly string CoreStylePath   = "UI/Views/Core/CoreStyle";
        private static readonly string CommonStylePath = "UI/Views/Common/CommonMiscStyle";
        private static readonly string TitleLocKey     = "CycleBenefit.ChooseTitle";

        private readonly PanelStack _panelStack;
        private readonly IAssetLoader _assetLoader;
        private readonly ILoc _loc;

        private List<IBenefit> _offered = new();
        private Action<IBenefit>? _onChosen;

        public BenefitSelectionPanel(PanelStack panelStack, IAssetLoader assetLoader, ILoc loc)
        {
            _panelStack  = panelStack;
            _assetLoader = assetLoader;
            _loc         = loc;
        }

        public void ShowFor(List<IBenefit> offered, Action<IBenefit> onChosen)
        {
            _offered  = offered;
            _onChosen = onChosen;
            _panelStack.PushDialog(this);
        }

        // ---------------------------------------------------------------
        // IPanelController
        // ---------------------------------------------------------------

        public VisualElement GetPanel() => BuildPanel();

        public bool OnUIConfirmed()
        {
            if (_offered.Count > 0) Choose(_offered[0]);
            return true;
        }

        public void OnUICancelled() { }

        // ---------------------------------------------------------------
        // Panel construction
        // ---------------------------------------------------------------

        private VisualElement BuildPanel()
        {
            var coreStyle   = _assetLoader.Load<StyleSheet>(CoreStylePath);
            var commonStyle = _assetLoader.Load<StyleSheet>(CommonStylePath);

            var wrapper = new VisualElement();
            wrapper.styleSheets.Add(coreStyle);
            wrapper.styleSheets.Add(commonStyle);
            wrapper.style.flexGrow       = 1;
            wrapper.style.alignItems     = Align.Center;
            wrapper.style.justifyContent = Justify.Center;

            var box = new NineSliceVisualElement();
            box.AddToClassList("options-box");
            box.AddToClassList("sliced-border");
            wrapper.Add(box);

            var title = new Label(_loc.T(TitleLocKey));
            title.style.color                   = Color.white;
            title.style.fontSize                = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom            = 12;
            title.style.alignSelf               = Align.Center;
            box.Add(title);

            foreach (var benefit in _offered)
                box.Add(BuildBenefitButton(benefit));

            return wrapper;
        }

        private VisualElement BuildBenefitButton(IBenefit benefit)
        {
            // Outer container styled as the menu button — handles background,
            // hover state, sizing, and click. We don't use Button.text so we
            // have full control over the interior layout.
            var button = new MenuButton();
            button.AddToClassList("menu-button");
            button.AddToClassList("menu-button--stretched");
            button.RegisterCallback<ClickEvent>(_ => Choose(benefit));

            // Interior row: icon on the left, label centred in remaining space.
            // Using an explicit row container inside the button gives us clean
            // flex control without fighting the button's own text element.
            var row = new VisualElement();
            row.style.flexDirection  = FlexDirection.Row;
            row.style.alignItems     = Align.Center;
            row.style.flexGrow       = 1;
            // Match the button's own padding so the row fills it edge-to-edge.
            row.style.paddingLeft    = 20;
            row.style.paddingRight   = 20;
            button.Add(row);

            // Icon
            if (benefit.IconPath != null)
            {
                var sprite = _assetLoader.LoadSafe<Sprite>(benefit.IconPath);
                if (sprite != null)
                {
                    var icon = new Image();
                    icon.sprite              = sprite;
                    icon.scaleMode           = ScaleMode.ScaleToFit;
                    icon.style.width         = 24;
                    icon.style.height        = 24;
                    icon.style.flexShrink    = 0;
                    icon.style.marginRight   = 10;
                    icon.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                    // Mirror the CSS hover colour change on the icon tint.
                    button.RegisterCallback<MouseEnterEvent>(_ =>
                        icon.style.unityBackgroundImageTintColor = new StyleColor(Color.black));
                    button.RegisterCallback<MouseLeaveEvent>(_ =>
                        icon.style.unityBackgroundImageTintColor = new StyleColor(Color.white));
                    row.Add(icon);
                }
            }

            // Label — centred in the remaining space, bold white matching menu-button style.
            // Color is set explicitly because child elements don't inherit color in UIToolkit.
            var label = new Label(benefit.GetDisplayName(_loc));
            label.style.flexGrow                = 1;
            label.style.unityTextAlign          = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize                = 14;
            label.style.color                   = new StyleColor(Color.white);
            button.RegisterCallback<MouseEnterEvent>(_ =>
                label.style.color = new StyleColor(Color.black));
            button.RegisterCallback<MouseLeaveEvent>(_ =>
                label.style.color = new StyleColor(Color.white));
            row.Add(label);

            return button;
        }

        private void Choose(IBenefit benefit)
        {
            Debug.Log($"[CycleBenefit] Player chose: {benefit.GetDisplayName(_loc)}");
            _panelStack.Pop(this);
            _onChosen?.Invoke(benefit);
        }
    }
}