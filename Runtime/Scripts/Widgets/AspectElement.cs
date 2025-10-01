using UnityEngine;
using UnityEngine.UIElements;

namespace Concept.UI
{
    [UxmlElement]
    public partial class AspectElement : VisualElement
    {
        private Texture2D _currentTexture;

        public AspectElement()
        {
            // Remove o BackgroundSize.Contain - isso que estava causando o problema
            // Queremos que a imagem mostre na proporção original, mesmo que corte
            style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
            style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
            style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
            style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            DetectTexture();
            UpdateAspectRatio();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            schedule.Execute(UpdateAspectRatio).ExecuteLater(0);
        }

        private void DetectTexture()
        {
            try
            {
                var bgValue = style.backgroundImage.value;
                if (bgValue != null && bgValue.texture != null)
                {
                    _currentTexture = bgValue.texture;
                }
            }
            catch
            {
                _currentTexture = null;
            }
        }

        private void UpdateAspectRatio()
        {
            DetectTexture(); // Sempre tenta detectar a textura atual

            if (_currentTexture == null)
            {
                style.height = StyleKeyword.Auto;
                return;
            }

            float width = resolvedStyle.width;

            // Se não tem width definido, usa valores alternativos
            if (width <= 0)
            {
                width = layout.width > 0 ? layout.width : this.worldBound.width;
                if (width <= 0) return;
            }

            // Calcula a altura baseada na proporção da imagem
            float aspectRatio = (float)_currentTexture.height / _currentTexture.width;
            float calculatedHeight = width * aspectRatio;

            if (calculatedHeight > 0 && !float.IsNaN(calculatedHeight))
            {
                style.height = calculatedHeight;

                // Força o redesenho
                this.MarkDirtyRepaint();
            }
        }


        // Método para forçar a atualização quando a textura mudar
        public void SetBackgroundTexture(Texture2D texture)
        {
            style.backgroundImage = texture;
            _currentTexture = texture;
            UpdateAspectRatio();
        }
    }
}