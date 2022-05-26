using Elsa.Dsl.Implementations;
using Elsa.Dsl.Services;
using Elsa.Expressions.Configuration;
using Elsa.JavaScript.Configuration;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Attributes;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.Configuration;

[Dependency(typeof(JavaScriptConfigurator))]
[Dependency(typeof(ExpressionsConfigurator))]
public class DslConfigurator : ConfiguratorBase
{
    public DslConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
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