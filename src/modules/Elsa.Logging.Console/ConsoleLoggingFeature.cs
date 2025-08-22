using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Logging.Console;
using Elsa.Logging.Contracts;
using Elsa.Logging.Features;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Logging;

/// <summary>
/// A feature that installs Console logging services.
/// </summary>
[DependsOn(typeof(LoggingFeature))]
public class ConsoleLoggingFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.AddScoped<ILogSinkFactory, ConsoleLogSinkFactory>();
    }
}