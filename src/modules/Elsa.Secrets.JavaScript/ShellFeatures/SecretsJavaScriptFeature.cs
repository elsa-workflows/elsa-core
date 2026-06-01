using CShells.Features;
using Elsa.Expressions.JavaScript.Extensions;
using Elsa.Expressions.JavaScript.ShellFeatures;
using Elsa.Secrets.JavaScript.Scripting.JavaScript;
using Elsa.Secrets.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.JavaScript.ShellFeatures;

[ShellFeature(
    DisplayName = "Secrets JavaScript",
    Description = "Provides JavaScript expression functions for resolving Elsa secrets.",
    DependsOn = [typeof(JavaScriptFeature), typeof(SecretsFeature)])]
[UsedImplicitly]
public class SecretsJavaScriptFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddNotificationHandler<SecretsJavaScriptHandler>()
            .AddFunctionDefinitionProvider<SecretsJavaScriptHandler>();
    }
}
