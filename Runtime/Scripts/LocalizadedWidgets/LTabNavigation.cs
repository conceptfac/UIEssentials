using Concept.Localization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

namespace Concept.UI
{
    [UxmlElement]
    public partial class LTabNavigation : TabNavigation
    {

        /// <summary>
        /// The string table collection to retrieve the localized value from.
        /// Defaults to "UI".
        /// </summary>
        [UxmlAttribute("collection")]
        public string collection { get; set; } = "UI";


        public LTabNavigation() 
        {
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                _ = UpdateTabButtonsAsync();
                SelectIndex(index);
                LocalizationProvider.RegisterUpdateAction(UpdateTabButtonsAsync);
            });



            // Remove the update action when the label is detached from the UI panel
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                LocalizationProvider.RemoveUpdateAction(UpdateTabButtonsAsync);
            });

        }


        protected override void UpdateTabButtons()
        {
            _ = UpdateTabButtonsAsync();
        }


        private async Task UpdateTabButtonsAsync()
        {

            Clear();


            for (int i = 0; i < m_tabButtons.Length; i++)
            {
                string key = m_tabButtons[i];
                var (success, text) = await LocalizationProvider.GetLocalizedStringAsync(collection, key);
                int id = i;
                var tabBt = new Button();
                tabBt.text = success ? text : key;
                tabBt.clicked += () => SelectIndex(id);
                tabBt.AddToClassList("tab-button");
                if (i == index)
                    tabBt.AddToClassList("active");
                else
                    tabBt.RemoveFromClassList("active");
                Add(tabBt);
            }

        }

    }
}
