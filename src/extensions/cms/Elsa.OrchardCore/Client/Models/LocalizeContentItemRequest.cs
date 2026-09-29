namespace Elsa.OrchardCore.Client.Models
{
    /// <summary>
    /// Represents a request to localize a content item in a specific culture.
    /// </summary>
    public class LocalizeContentItemRequest
    {
        /// <summary>
        /// Gets or sets the code of the target culture for localization.
        /// The culture code should follow the standard format (e.g., "en-US" or "fr-FR").
        /// </summary>
        public string CultureCode { get; set; } = null!;
    }
}