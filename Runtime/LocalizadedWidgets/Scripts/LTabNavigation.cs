using Concept.Localization;
using System.Collections.Generic;
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
                    LocalizationProvider.RegisterUpdateAction(UpdateTabButtonsAsync);
                    SelectIndex(index);
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

            List<string> captions = new List<string>();
            for (int i = 0; i < m_tabButtons.Length; i++)
            {
                string key = m_tabButtons[i];
                var (success, text) = await LocalizationProvider.GetLocalizedStringAsync(collection, key);
                captions.Add(success ? text : key);
            }
            Clear();
            for (int i = 0; i < captions.Count; i++)
            {
                int id = i;
                var tabBt = new Button();
                tabBt.text = captions[i];
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
