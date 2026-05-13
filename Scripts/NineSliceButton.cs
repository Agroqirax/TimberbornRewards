using System;
using Timberborn.CoreUI;
using UnityEngine.UIElements;

namespace Agroqirax.Benefits
{
    /// <summary>
    /// Mirrors the game's internal <c>NineSliceButton</c>, which is inaccessible
    /// to mod code. Needed because <c>LocalizableButton</c> (the public alternative)
    /// requires a <c>text-loc-key</c> attribute and cannot be used for dynamically
    /// labelled buttons. Behaviour is identical to the game's version.
    /// </summary>
    internal class NineSliceButton : Button
    {
        private readonly NineSliceBackground _nineSliceBackground = new NineSliceBackground();

        public NineSliceButton()
        {
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