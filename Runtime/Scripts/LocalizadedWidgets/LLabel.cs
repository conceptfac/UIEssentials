using Concept.Localization;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Concept.UI
{
    /// <summary>
    /// A localized UI label that automatically updates its text
    /// based on the current locale using a specified collection and key.
    /// </summary>
    [UxmlElement]
    public partial class LLabel : Label
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
        /// Constructs a new <see cref="LLabel"/> and registers
        /// an update action to refresh its text when the locale changes.
        /// </summary>
        public LLabel()
        {
            LocalizationProvider.RegisterUpdateAction(UpdateText);

            // Remove the update action when the label is detached from the UI panel
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                LocalizationProvider.RemoveUpdateAction(UpdateText);
            });
        }

        /// <summary>
        /// Asynchronously updates the label text based on the current locale.
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
