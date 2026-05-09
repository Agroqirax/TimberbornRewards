using System;
using System.Collections.Generic;
using Timberborn.CoreUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Modal panel shown at cycle start. Presents 3 benefit options as buttons;
    /// the player must click one — there is no dismiss/cancel (Mini Metro style).
    ///
    /// Pushed onto the PanelStack via PushDialog, which automatically:
    ///   - wraps us in the game's standard darkened overlay
    ///   - pauses game speed
    ///   - hooks up the input system
    /// </summary>
    public class BenefitSelectionPanel : IPanelController
    {
        // CSS classes borrowed from the game's own entity panel / dialog styling.
        // These give us the standard Timberborn look for free.
        private static readonly string PanelClass        = "entity-sub-panel";
        private static readonly string PanelBgClass      = "bg-sub-box--brown";
        private static readonly string TitleClass        = "text--bold";
        private static readonly string ButtonClass       = "entity-fragment__button";
        private static readonly string ButtonGreenClass  = "entity-fragment__button--green";

        private readonly PanelStack _panelStack;

        // Set before pushing — the panel reads these to build its buttons.
        private List<IBenefit> _offered = new();
        private Action<IBenefit>? _onChosen;

        private VisualElement? _root;

        public BenefitSelectionPanel(PanelStack panelStack)
        {
            _panelStack = panelStack;
        }

        /// <summary>
        /// Call this to configure the panel's contents, then push it onto the stack.
        /// </summary>
        public void ShowFor(List<IBenefit> offered, Action<IBenefit> onChosen)
        {
            _offered  = offered;
            _onChosen = onChosen;
            _root     = null;   // force a fresh build next GetPanel() call
            _panelStack.PushDialog(this);
        }

        // ---------------------------------------------------------------
        // IPanelController
        // ---------------------------------------------------------------

        public VisualElement GetPanel()
        {
            _root = BuildPanel();
            return _root;
        }

        /// <summary>
        /// Keyboard confirm (Enter/Gamepad A) selects the first option,
        /// mirroring the auto-select behaviour we had before.
        /// </summary>
        public bool OnUIConfirmed()
        {
            if (_offered.Count > 0)
                Choose(_offered[0]);
            return true;  // true = play confirm sound
        }

        /// <summary>
        /// Cancel is a no-op — the player must make a choice.
        /// </summary>
        public void OnUICancelled() { }

        // ---------------------------------------------------------------
        // Panel construction
        // ---------------------------------------------------------------

        private VisualElement BuildPanel()
        {
            // Outer wrapper — centred by the overlay, sized to content
            var panel = new NineSliceVisualElement();
            panel.AddToClassList(PanelClass);
            panel.AddToClassList(PanelBgClass);
            panel.style.paddingTop    = 16;
            panel.style.paddingBottom = 16;
            panel.style.paddingLeft   = 24;
            panel.style.paddingRight  = 24;
            panel.style.minWidth      = 320;

            // Title label
            var title = new Label("Choose a Cycle Benefit");
            title.AddToClassList(TitleClass);
            title.style.fontSize      = 18;
            title.style.marginBottom  = 12;
            title.style.alignSelf     = Align.Center;
            panel.Add(title);

            // One button per offered benefit
            for (int i = 0; i < _offered.Count; i++)
            {
                IBenefit benefit = _offered[i];  // capture for lambda

                var button = new Button();
                button.text = benefit.DisplayName;
                button.AddToClassList(ButtonClass);
                button.AddToClassList(ButtonGreenClass);
                button.style.marginTop    = 4;
                button.style.marginBottom = 4;
                button.style.color        = UnityEngine.Color.white;
                button.RegisterCallback<ClickEvent>(_ => Choose(benefit));

                panel.Add(button);
            }

            return panel;
        }

        private void Choose(IBenefit benefit)
        {
            Debug.Log($"[CycleBenefit] Player chose: {benefit.DisplayName}");
            _panelStack.Pop(this);
            _onChosen?.Invoke(benefit);
        }
    }
}
