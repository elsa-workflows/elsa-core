using CShells.Features;
using Elsa.Expressions.JavaScript.ShellFeatures;
using Elsa.Http.Scripting.JavaScript;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.ShellFeatures;

/// <summary>
/// Provides JavaScript integration for HTTP features.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.HTTP)]
[ManifestFeatureCategory(ManifestFeatureCategories.Scripting)]
[ShellFeature(
    DisplayName = "HTTP JavaScript",
    Description = "Provides JavaScript integration for HTTP activities",
    DependsOn = [typeof(JavaScriptFeature)])]
[UsedImplicitly]
public class HttpJavaScriptFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddNotificationHandler<HttpJavaScriptHandler>();
    }
}


