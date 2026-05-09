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
            _panelStack = panelStack;
            _assetLoader = assetLoader;
            _loc = loc;
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

        public VisualElement GetPanel()  => BuildPanel();
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
            title.style.color                       = Color.white;
            title.style.fontSize                    = 18;
            title.style.unityFontStyleAndWeight     = FontStyle.Bold;
            title.style.marginBottom                = 12;
            title.style.alignSelf                   = Align.Center;
            box.Add(title);

            foreach (var benefit in _offered)
                box.Add(BuildBenefitButton(benefit));

            return wrapper;
        }

        private VisualElement BuildBenefitButton(IBenefit benefit)
        {
            // Use button.text so the game's CSS hover colour applies to the
            // text directly — child Label elements don't inherit color in UIToolkit.
            var button = new MenuButton();
            button.AddToClassList("menu-button");
            button.AddToClassList("menu-button--stretched");
            button.text = benefit.GetDisplayName(_loc);
            button.style.flexDirection  = FlexDirection.Row;
            button.style.alignItems     = Align.Center;
            button.style.justifyContent = Justify.Center;
            button.RegisterCallback<ClickEvent>(_ => Choose(benefit));

            // Icon — placed as a sibling to the button's internal text element.
            // We insert it at index 0 so it appears to the left of the text.
            if (benefit.IconPath != null)
            {
                var sprite = _assetLoader.LoadSafe<Sprite>(benefit.IconPath);
                if (sprite != null)
                {
                    var icon = new Image();
                    icon.sprite              = sprite;
                    icon.style.width         = 24;
                    icon.style.height        = 24;
                    icon.style.marginRight   = 10;
                    icon.style.flexShrink    = 0;
                    icon.scaleMode           = ScaleMode.ScaleToFit;
                    // Image tint follows the button's current text colour so it
                    // flips white↔black on hover automatically.
                    icon.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                    button.RegisterCallback<MouseEnterEvent>(_ =>
                        icon.style.unityBackgroundImageTintColor = new StyleColor(Color.black));
                    button.RegisterCallback<MouseLeaveEvent>(_ =>
                        icon.style.unityBackgroundImageTintColor = new StyleColor(Color.white));
                    button.Insert(0, icon);
                }
            }

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
