using Concept.Localization;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Concept.UI
{
    /// <summary>
    /// A localized UI button that automatically updates its text
    /// based on the current locale using a specified collection and key.
    /// </summary>
    [UxmlElement]
    public partial class LButton : Button
    {
        /// <summary>
        /// The string table collection to retrieve the localized value from.
        /// Defaults to "UI".
        /// </summary>
        [UxmlAttribute("collection")]
        public string collection { get; set; } = "UI";

        private string m_key;

        /// <summary>
        /// The key of the localized entry within the collection.
        /// Setting this property triggers an automatic text update.
        /// </summary>
        [UxmlAttribute("key")]
        public string key
        {
            get => m_key;
            set
            {
                m_key = value;
                _ = UpdateText();
            }
        }

        /// <summary>
        /// Constructs a new <see cref="LButton"/> and registers
        /// an update action to refresh its text when the locale changes.
        /// </summary>
        public LButton()
        {
            LocalizationProvider.RegisterUpdateAction(UpdateText);

            // Remove the update action when the button is detached from the UI panel
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                LocalizationProvider.RemoveUpdateAction(UpdateText);
            });
        }

        /// <summary>
        /// Asynchronously updates the button text based on the current locale.
        /// </summary>
        private async Task UpdateText()
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(collection))
                return;

            var (success, text) = await LocalizationProvider.GetLocalizedStringAsync(collection, key);
            this.text = success ? text : key;
        }
    }
}
