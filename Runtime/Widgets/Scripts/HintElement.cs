using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Concept.UI
{
    [UxmlElement]
    public partial class HintElement : Label
    {
        private const string USS_CLASS_NAME = "hint-element";
        private bool m_active;
        [UxmlAttribute("active")]
        public bool active
        {
            get => m_active;
            set
            {
                m_active = value;
            EnableInClassList("active",value);
            }
        }

        private IVisualElementScheduledItem m_hideScheduler;
        public HintElement()
        {
            text = "HintElement";
            var className = GetType().Name;
            styleSheets.Add(Resources.Load<StyleSheet>($"Widgets/{className}/{className}Styles"));
            AddToClassList(USS_CLASS_NAME);
        }



        public void Pop(string hintText, Vector2 position)
        {
            if(m_hideScheduler != null) { m_hideScheduler.Pause(); m_hideScheduler = null; }

            style.position = Position.Absolute;
            style.left = position.x;
            style.top = position.y + 15;

            text = hintText;
            style.display = DisplayStyle.Flex;
            active = true;
        }

        public void Hide()
        {
            active = false;

            var durations = resolvedStyle.transitionDuration;
            var delays = resolvedStyle.transitionDelay;
            float duration = durations.Count() > 0 ? durations.First().value : 0f;
            float delay = delays.Count() > 0 ? delays.First().value : 0f;
            float totalTime = duration + delay;

            m_hideScheduler = schedule.Execute(() =>
            {
                style.display = DisplayStyle.None;
            }).StartingIn((int)(totalTime * 1000));

        }
    }


    public struct HintData
    {
        public string hintText;

        public HintData(string hintText)
        {
            this.hintText = hintText;
        }
    }


    public static class HintElementExtensions
    {
        public static HintElement currentHint { get; private set; }

        public static void SetHint(this HintElement hintElement) => currentHint = hintElement;
        public static void RegisterHint(this VisualElement element, HintData hintData) => RegisterHint(element,hintData, currentHint);
        public static void RegisterHint(this VisualElement element, HintData hintData, HintElement hintElement) {
            // Registra callbacks:

            void OnPointerEvent(PointerEnterEvent evt)
            {
                var worldPos = element.LocalToWorld(evt.localPosition);
                hintElement.Pop(hintData.hintText,worldPos);
            }

            element.RegisterCallback<PointerEnterEvent>(OnPointerEvent);

            element.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                hintElement.Hide();
            });

        }


        public static void RegisterHint(this VisualElement element, EventCallback<PointerEnterEvent> hintEvent)
        {
            element.RegisterCallback(hintEvent);

            element.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                currentHint.Hide();
            });

        }

        public static void Pop(string hintText, Vector2 position)
        {
            currentHint.Pop(hintText, position);
        }

    }

}
