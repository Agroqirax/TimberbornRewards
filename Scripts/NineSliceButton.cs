using System;
using Timberborn.CoreUI;
using UnityEngine.UIElements;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// A Button that renders its background via NineSliceBackground, exactly
    /// like the game's own internal NineSliceButton / LocalizableButton.
    /// This lets us use "menu-button" and other USS classes that rely on
    /// --background-image and --background-slice-* custom properties.
    /// </summary>
    public class MenuButton : Button
    {
        private readonly NineSliceBackground _nineSliceBackground = new NineSliceBackground();

        public MenuButton()
        {
            // Replicate the constructor ordering from NineSliceButton exactly:
            // move the default generateVisualContent delegates to the end so
            // NineSlice draws first (underneath the label text).
            Delegate[] existing = generateVisualContent.GetInvocationList();
            generateVisualContent = (Action<MeshGenerationContext>)Delegate.Combine(
                generateVisualContent,
                new Action<MeshGenerationContext>(OnGenerateVisualContent));
            foreach (Delegate d in existing)
            {
                generateVisualContent = (Action<MeshGenerationContext>)Delegate.Remove(
                    generateVisualContent, (Action<MeshGenerationContext>)d);
                generateVisualContent = (Action<MeshGenerationContext>)Delegate.Combine(
                    generateVisualContent, (Action<MeshGenerationContext>)d);
            }
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            _nineSliceBackground.GetDataFromStyle(customStyle);
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            _nineSliceBackground.GenerateVisualContent(mgc, paddingRect);
        }
    }
}