using Elsa.Dsl.Contracts;
using Elsa.Dsl.Services;
using Elsa.Expressions.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.Features;

[DependsOn(typeof(JavaScriptFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class DslFeature : FeatureBase
{
    public DslFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Services
            .AddSingleton<IDslEngine, DslEngine>()
            .AddSingleton<ITypeSystem, TypeSystem>()
            .AddSingleton<IFunctionActivityRegistry, FunctionActivityRegistry>();
    }
}