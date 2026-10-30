using System.Text.Json.Nodes;

namespace Elsa.OrchardCore.Client.Models
{
    /// <summary>
    /// Represents a request to apply a JSON patch to an existing content item in a content management system.
    /// </summary>
    public class PatchContentItemRequest
    {
        /// <summary>
        /// Gets or sets the JSON patch document containing the changes to be applied to the content item.
        /// </summary>
        public JsonNode Patch { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the updated content item should be published
        /// after the patch is applied. If set to <c>true</c>, the item will be published; otherwise, it will remain in draft.
        /// </summary>
        public bool Publish { get; set; }
    }
}