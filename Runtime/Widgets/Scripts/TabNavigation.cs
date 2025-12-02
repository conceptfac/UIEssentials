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

        private List<(string, VisualElement)> m_tabsContent = new List<(string, VisualElement)>();


        public int index = 0;

        protected List<string> m_tabButtons = new List<string>();

        [UxmlAttribute("tab-buttons")]
        public List<string> tabButtons
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
            //  Debug.LogWarning(this.name + ":" + m_tabButtons.Length + " (BASE)");



            Clear();
            if (m_tabButtons == null) return;
            for (int i = 0; i < m_tabButtons.Count; i++)
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

            for (int i = 0; i < m_tabsContent.Count; i++)
            {
                m_tabsContent[i].Item2.style.display = (i == index) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            OnTabSelect?.Invoke(index);
        }

        public List<Button> GetButtonsList() => this.Query<Button>().ToList();

        public void SetTabsContent(List<(string, VisualElement)> tabsContent)
        {
            ClearTabs();
            foreach (var item in tabsContent) AddTab(item);
            SelectIndex(0);
        }

        public void AddTab((string, VisualElement) tab)
        {
            m_tabButtons.Add(tab.Item1);
            m_tabsContent .Add(tab);
        }


        public void RemoveTab(int tabIndex)
        {
            throw new NotImplementedException();
        }

        public void RemoveTab(string tabName)
        {
            throw new NotImplementedException();
        }
        public void ClearTabs()
        {
            m_tabButtons.Clear();
            m_tabsContent.Clear();
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