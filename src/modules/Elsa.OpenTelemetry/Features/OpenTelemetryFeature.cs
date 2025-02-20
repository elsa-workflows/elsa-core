using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.OpenTelemetry.Contracts;
using Elsa.OpenTelemetry.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.OpenTelemetry.Features;

public class OpenTelemetryFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Services
            .AddScoped<IErrorSpanHandler, DefaultErrorSpanHandler>()
            .AddScoped<IErrorSpanHandler, FaultExceptionErrorSpanHandler>();
    }
}