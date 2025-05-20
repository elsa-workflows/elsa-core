using Elsa.Expressions.Dsl.Contracts;
using Elsa.Expressions.Dsl.Services;
using Elsa.Expressions.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Dsl.Features;

/// <inheritdoc />
[DependsOn(typeof(ExpressionsFeature))]
public class DslFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Services
            .AddScoped<IDslEngine, DslEngine>()
            .AddScoped<ITypeSystem, TypeSystem>()
            .AddScoped<IFunctionActivityRegistry, FunctionActivityRegistry>();
    }
}