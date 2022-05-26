using Elsa.Expressions.Implementations;
using Elsa.Expressions.Services;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Configuration;

public class ExpressionsConfigurator : ConfiguratorBase
{
    public ExpressionsConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }

    public override void Configure()
    {
        Services
            .AddSingleton<IExpressionEvaluator, ExpressionEvaluator>()
            .AddSingleton<IExpressionHandlerRegistry, ExpressionHandlerRegistry>();
    }
}