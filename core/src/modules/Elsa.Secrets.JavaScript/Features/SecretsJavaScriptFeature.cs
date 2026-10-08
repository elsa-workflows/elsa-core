using Elsa.Expressions.JavaScript.Extensions;
using Elsa.Expressions.JavaScript.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Secrets.Features;
using Elsa.Secrets.JavaScript.Scripting.JavaScript;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.JavaScript.Features;

[DependsOn(typeof(JavaScriptFeature))]
[DependsOn(typeof(SecretsFeature))]
public class SecretsJavaScriptFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services
            .AddNotificationHandler<SecretsJavaScriptHandler>()
            .AddFunctionDefinitionProvider<SecretsJavaScriptHandler>();
    }
}
