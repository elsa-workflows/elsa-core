using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Features;
using Elsa.JavaScript.Extensions;
using Elsa.Secrets.Management.Features;
using Elsa.Secrets.Scripting.JavaScript;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Scripting.Features;

[DependencyOf(typeof(SecretManagementFeature))]
[DependencyOf(typeof(JavaScriptFeature))]
public class SecretsScriptingFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Services.AddHandlersFrom<SecretsScriptingFeature>();
        Services.AddScoped<SecretsTypeDefinitionProvider>();
        Services.AddTypeDefinitionProvider<SecretsTypeDefinitionProvider>(sp => sp.GetRequiredService<SecretsTypeDefinitionProvider>());
        Services.AddVariableDefinitionProvider<SecretsTypeDefinitionProvider>(sp => sp.GetRequiredService<SecretsTypeDefinitionProvider>());
    }
}