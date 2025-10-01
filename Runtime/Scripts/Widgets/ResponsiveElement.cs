using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Concept.UI
{

    [UxmlElement]
    public partial class ResponsiveElement : VisualElement
    {
        private Vector2 m_lastResolution;

        public Action<bool> OnResize;

        public ResponsiveElement()
        {
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            ResizeLayout();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ResizeLayout();
        }


        private void ResizeLayout()
        {
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;

            Vector2 newRes = new Vector2(w, h);

            if (newRes != m_lastResolution)
            {
                m_lastResolution = newRes;
                bool isLandscape = w > h;
                EnableInClassList("portrait", !isLandscape);
                OnResize?.Invoke(isLandscape);
            }
        }
    }
}
