using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Concept.UI
{
    [UxmlElement]
    public partial class CustomToggle : Toggle
    {
        private const string USSClassName = "custom-toggle";

        private VisualElement m_toggleButton;

        public void SetValue(bool value)
        {
            this.value = value;
            UpdateVisualState();
            OnToggleChanged?.Invoke(value);
        }

        private void UpdateVisualState()
        {
            if (m_toggleButton == null)
                return;

            VisualElement toggleIcon = m_toggleButton.Q<VisualElement>("ToggleIco");

            if (value)
            {
                m_toggleButton.AddToClassList("on");
                m_toggleButton.RemoveFromClassList("off");
                labelElement.AddToClassList("active");
                toggleIcon?.AddToClassList("active");
            }
            else
            {
                m_toggleButton.AddToClassList("off");
                m_toggleButton.RemoveFromClassList("on");
                labelElement.RemoveFromClassList("active");
                toggleIcon?.RemoveFromClassList("active");
            }
        }

        [UxmlAttribute("collection")]
        public string collection { get; set; } = "UI";

        private string m_key;

        [UxmlAttribute("key")]
        public string key
        {
            get => m_key;
            set => m_key = value;
        }

        public event Action<bool> OnToggleChanged;

        public CustomToggle()
        {
            AddToClassList(USSClassName);

            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("Widgets/CustomToggle");
            if (visualTree == null)
            {
                Debug.LogError("CustomToggle not found in Resources.");
                return;
            }

            visualTree.CloneTree(this);

            VisualElement checkMark = this.Q<VisualElement>("unity-checkmark")?.parent;
            if (checkMark != null)
                checkMark.style.display = DisplayStyle.None;

            m_toggleButton = this.Q<VisualElement>("ToggleButton");
            if (m_toggleButton != null)
            {
                m_toggleButton.RegisterCallback<ClickEvent>(_ => { value = !value; });
            }

            this.RegisterValueChangedCallback(_ =>
            {
                UpdateVisualState();
                OnToggleChanged?.Invoke(value);
            });

            styleSheets.Add(Resources.Load<StyleSheet>($"Widgets/{GetType().Name}Styles"));

            this.RegisterCallback<AttachToPanelEvent>(_ => { UpdateVisualState(); });
        }

        public void AnimateToPosition(float targetX, float targetY, int durationMs = 300,
            Action onComplete = null, Func<float, float> easing = null)
        {
            style.position = Position.Absolute;

            Vector2 currentPos = GetRelativePositionToParent();
            style.left = currentPos.x;
            style.top = currentPos.y;

            schedule.Execute(() =>
            {
                experimental.animation
                    .Start(new StyleValues
                    {
                        left = targetX,
                        top = targetY
                    }, durationMs)
                    .Ease(easing ?? Easing.OutQuad)
                    .OnCompleted(() => onComplete?.Invoke());
            }).StartingIn(0);
        }

        private Vector2 GetRelativePositionToParent()
        {
            if (parent == null)
                return Vector2.zero;

            Vector3 worldPos = worldTransform.MultiplyPoint3x4(Vector3.zero);
            Vector3 parentWorldPos = parent.worldTransform.MultiplyPoint3x4(Vector3.zero);

            return new Vector2(
                worldPos.x - parentWorldPos.x,
                worldPos.y - parentWorldPos.y);
        }
    }
}
