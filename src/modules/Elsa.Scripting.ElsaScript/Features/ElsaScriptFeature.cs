using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.ElsaScript.Features;

[DependsOn(typeof(Elsa.Workflows.Core.Features.WorkflowsFeature))]
public class ElsaScriptFeature : FeatureBase
{
    public ElsaScriptFeature(IModule module) : base(module)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ElsaScriptCompiler>();
    }
}
