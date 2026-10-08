using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Logging.Contracts;
using Elsa.Logging.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Logging.Serilog;

/// <summary>
/// A feature that installs Serilog logging services for Elsa.
/// </summary>
[DependsOn(typeof(LoggingFeature))]
public class SerilogLoggingFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc/>
    public override void Apply()
    {
        Services
            .AddScoped<ILogSinkFactory, SerilogLogSinkFactory>();
    }
}