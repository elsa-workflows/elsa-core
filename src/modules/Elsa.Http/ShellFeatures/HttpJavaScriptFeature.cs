using CShells.Features;
using Elsa.Http.Scripting.JavaScript;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.ShellFeatures;

/// <summary>
/// Enabled when both HTTP and JavaScript features are enabled.
/// </summary>
[ShellFeature(
    DisplayName = "HTTP JavaScript Integration",
    Description = "Provides JavaScript integration for HTTP activities",
    DependencyOf = ["Http", "JavaScript"])]
public class HttpJavaScriptFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddNotificationHandler<HttpJavaScriptHandler>();
    }
}
