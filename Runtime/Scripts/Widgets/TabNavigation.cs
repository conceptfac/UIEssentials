using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Concept.UI
{
    [Serializable]
    public struct TabButton
    {
        public string text;
        public string collection;
        public string key;
    }

    [UxmlElement]
    public partial class TabNavigation : VisualElement
    {
        private const string USSClassName = "tab-navigation";

        private readonly VisualElement m_root;
        public VisualElement Root => m_root;

        public int index = 0;

        private string[] m_tabButtons;

        [UxmlAttribute("tab-buttons")]
        public string[] tabButtons
        {
            get => m_tabButtons; set
            {
                if (m_tabButtons == value) return;
                m_tabButtons = value;
                UpdateTabButtons();
            }
        }
        public Action<int> OnTabSelect;

        public TabNavigation()
        {
            AddToClassList(USSClassName);

            var visualTree = Resources.Load<VisualTreeAsset>("Widgets/TabNavigation");
            if (visualTree == null)
            {
                Debug.LogError("[TabNavigation] Fatal Error: 'TabNavigation' UXML not found.");
                return;
            }

            m_root = visualTree.CloneTree();
            m_root.AddToClassList(USSClassName);
            hierarchy.Add(m_root);


            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                 UpdateTabButtons();
            });

        }


        void UpdateTabButtons()
        {
            m_root.Clear();

            for (int i = 0; i < m_tabButtons.Length; i++)
            {
                int id = i;
                var tabBt = new LButton();
                tabBt.collection = "UI";
                tabBt.key = m_tabButtons[i];

                tabBt.clicked += () => OnTabButtonClicked(id);
                tabBt.AddToClassList("tab-button");
                if (i == index)
                    tabBt.AddToClassList("active");
                else
                    tabBt.RemoveFromClassList("active");
                m_root.Add(tabBt);
            }

        }

        void OnTabButtonClicked(int id)
        {
            index = id;
            UpdateTabButtons();
            OnTabSelect?.Invoke(id);

        }
    }

    [UxmlElement]
    public partial class TabBt : VisualElement
    {
        private const string USSClassName = "tab-button";
        public Button button;
        public Action onClick;
        public TabBt()
        {
            AddToClassList(USSClassName);
            button = new Button();
            Add(button);
        }
    }
}