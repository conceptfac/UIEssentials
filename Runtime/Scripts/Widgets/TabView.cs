using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Concept.UI
{
    [UxmlElement("TabView")]
    public partial class TabView : VisualElement
    {
        public readonly struct Tab
        {
            public readonly string name;
            public readonly string title;
            public readonly VisualElement content;

            public Tab(string name, string title, VisualElement content)
            {
                this.name = name;
                this.title = title;
                this.content = content;
            }

        }

        public VisualElement header { get; private set; }
        public VisualElement content { get; private set; }

        public TabView() {
            header = this.Q<VisualElement>("header");
            content = this.Q<VisualElement>("content");
        }

        public void CreateTab(Tab tab) {
            if (header == null || content == null)
            {
                Debug.LogError("[TabView] Header or Content container not found.");
                return;
            }

            var button = new Button(() => SelectTab(tab.name))
            {
                text = tab.title,
                name = $"tab-button-{tab.name}"
            };

            // Adiciona o botão no header
            header.Add(button);

            // Marca o conteúdo da aba com um nome único
            tab.content.name = $"tab-content-{tab.name}";
            tab.content.style.display = DisplayStyle.None;

            // Adiciona o conteúdo da aba no container
            content.Add(tab.content);
        }

        public void SelectTab(string name)
        {
            foreach (var child in content.Children())
            {
                child.style.display = DisplayStyle.None;
            }

            var tabToShow = content.Q<VisualElement>($"tab-content-{name}");
            if (tabToShow != null)
                tabToShow.style.display = DisplayStyle.Flex;
        }
    }
}