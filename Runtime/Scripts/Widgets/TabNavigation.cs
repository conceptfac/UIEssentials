using System;
using System.Collections.Generic;
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


        public int index = 0;

        protected string[] m_tabButtons;

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

            visualTree.CloneTree(this);
            AddToClassList(USSClassName);
            
            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                 UpdateTabButtons();
            });

        }


        protected virtual void UpdateTabButtons()
        {
            Debug.LogWarning(this.name + ":" + m_tabButtons.Length + " (BASE)");

            Clear();

            for (int i = 0; i < m_tabButtons.Length; i++)
            {
                int id = i;
                var tabBt = new Button();
                tabBt.text = m_tabButtons[i];
                tabBt.clicked += () => SelectIndex(id);
                tabBt.AddToClassList("tab-button");
                if (i == index)
                    tabBt.AddToClassList("active");
                else
                    tabBt.RemoveFromClassList("active");
                Add(tabBt);
            }

        }

        public void SelectIndex(int index)
        {
            this.index = index;
            UpdateTabButtons();
            OnTabSelect?.Invoke(index);

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