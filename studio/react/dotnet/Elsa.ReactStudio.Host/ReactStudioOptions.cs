namespace Elsa.ReactStudio.Host;

/// <summary>Options for opting in to the React Studio Preview alongside the existing Studio.</summary>
public sealed class ReactStudioOptions
{
    /// <summary>The site-relative mount. Defaults to /studio-preview.</summary>
    public string Path { get; set; } = "/studio-preview";

    /// <summary>Fallback configuration file, relative to the application content root.</summary>
    public string ConfigurationFile { get; set; } = "studio.config.json";
}
