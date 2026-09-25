namespace Elsa.OrchardCore.Options
{
    /// <summary>
    /// Represents the configuration options for integrating with Orchard Core.
    /// </summary>
    public class OrchardCoreOptions
    {
        /// <summary>
        /// Gets or sets the collection of content types supported by the integration.
        /// Each content type is represented as a unique string.
        /// </summary>
        public ISet<string> ContentTypes { get; set; } = new HashSet<string>();
    }
}