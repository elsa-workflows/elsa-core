namespace Elsa.OrchardCore.Client.Models
{
    /// <summary>
    /// Represents a request to resolve a collection of tags in a content management system.
    /// </summary>
    public class ResolveTagsRequest
    {
        /// <summary>
        /// Gets or sets the collection of tags to be resolved.
        /// Each tag is represented as a string.
        /// </summary>
        public ICollection<string> Tags { get; set; } = [];
    }
}