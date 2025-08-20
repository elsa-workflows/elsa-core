using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Logging.Contracts;
using Elsa.Logging.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Logging.Features;

/// <summary>
/// A feature that installs Logging services for Elsa.
/// </summary>
public class ProcessLoggingFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        // ProcessLog Services.
        Services.AddScoped<ILogSinkResolver, DefaultLogSinkResolver>();
        Services.AddScoped<ILogSink, ConsoleLogSink>();
    }
}