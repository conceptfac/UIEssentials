using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
//[assembly: UxmlNamespacePrefix("Quantum.UI", "custom")]

namespace Concept.UI
{

    [UxmlElement]
    public partial class CustomToggle : Toggle
    {
        private const string USSClassName = "custom-toggle";

        private VisualElement m_toggleButton;
        

        /*
        private bool m_isLabelLeft = true;


        [UxmlAttribute("is-label-left")]
        public bool IsLabelLeft
        {
            get => m_isLabelLeft;
            set
            {
                if (m_isLabelLeft == value) return;
                m_isLabelLeft = value;
                UpdateLabelPosition();
                MarkDirtyRepaint();
            }
        }
     */

        public void SetValue(bool value)
        {
            this.value = value;
            UpdateVisualState();
            OnToggleChanged?.Invoke(value);
        }

        private void UpdateVisualState()
        {
            if (m_toggleButton == null) return;

            // Atualiza a direção do flex
            m_toggleButton.style.flexDirection = value ?
                FlexDirection.RowReverse :
                FlexDirection.Row;

            // Atualiza as classes CSS
            if (value)
            {
                labelElement.AddToClassList("active");
                m_toggleButton.Q<VisualElement>("ToggleIco").AddToClassList("active");
            }
            else
            {
                labelElement.RemoveFromClassList("active");
                m_toggleButton.Q<VisualElement>("ToggleIco").RemoveFromClassList("active");
            }
        }

        /*
        // MÉTODO NECESSÁRIO PARA BINDING
        public void SetValueWithoutNotify(bool newValue)
        {
            if (value == newValue) return;

            value = newValue;
            UpdateVisualState();  // Apenas atualiza visual, SEM disparar eventos
            MarkDirtyRepaint();
        }
        */

        [UxmlAttribute("collection")]
        public string collection { get; set; } = "UI";
        private string m_key;


        [UxmlAttribute("key")]
        public string key
        {
            get => m_key; set
            {
                m_key = value;
               // UpdateText(LocalizationSettings.SelectedLocale);
            }
        }

        public event Action<bool> OnToggleChanged;

        public CustomToggle()
        {
            AddToClassList(USSClassName);

            var visualTree = Resources.Load<VisualTreeAsset>("Widgets/CustomToggle");
            if (visualTree == null)
            {
                Debug.LogError("CustomToggle não encontrado em Resources!");
                return;
            }

            visualTree.CloneTree(this);

            var checkMark = this.Q<VisualElement>("unity-checkmark").parent;
            checkMark.style.display = DisplayStyle.None;
            m_toggleButton = this.Q<VisualElement>("ToggleButton");

            m_toggleButton.RegisterCallback<ClickEvent>(evt =>
            {
                value = !value;
            });



            this.RegisterValueChangedCallback((evt) => {

                UpdateVisualState();
                OnToggleChanged?.Invoke(value);
            });

            styleSheets.Add(Resources.Load<StyleSheet>("Widgets/"+ GetType().Name + "Styles"));

            /*
            LocalizationSettings.SelectedLocaleChanged += UpdateText;
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                LocalizationSettings.SelectedLocaleChanged -= UpdateText;
            });
            */

            this.RegisterCallback<AttachToPanelEvent>(evt => { 
            
                UpdateVisualState();
            
            });

        }


        

        /*
        private void UpdateText(Locale locale)
        {
            return;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(collection)) return;

           // m_label.SetLocalizationText(collection, key);
        }
        */


        public void AnimateToPosition(float targetX, float targetY, int durationMs = 300,
    Action onComplete = null, Func<float, float> easing = null)
        {
            // 1. Primeiro forçamos a posição absoluta
            style.position = Position.Absolute;

            // 2. Pegamos a posição VISUAL atual relativa ao parent
            var currentPos = this.GetRelativePositionToParent();

            // 3. Definimos explicitamente a posição inicial
            style.left = currentPos.x;
            style.top = currentPos.y;

            // 4. Esperamos o próximo frame para garantir que o layout foi atualizado
            schedule.Execute(() =>
            {
                // 5. Agora sim iniciamos a animação
                this.experimental.animation
                    .Start(new StyleValues
                    {
                        left = targetX,
                        top = targetY
                    }, durationMs)
                    .Ease(easing ?? Easing.OutQuad)
                    .OnCompleted(() => onComplete?.Invoke());
            }).StartingIn(0); // Próximo frame
        }

        // Método auxiliar para pegar posição relativa
        private Vector2 GetRelativePositionToParent()
        {
            if (parent == null) return Vector2.zero;

            // Calcula a posição relativa considerando transforms e pivots
            var worldPos = worldTransform.MultiplyPoint3x4(Vector3.zero);
            var parentWorldPos = parent.worldTransform.MultiplyPoint3x4(Vector3.zero);

            return new Vector2(
                worldPos.x - parentWorldPos.x,
                worldPos.y - parentWorldPos.y
            );
        }



    }

}