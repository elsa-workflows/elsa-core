using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Logging.Activities;
using Elsa.Logging.Contracts;
using Elsa.Logging.Options;
using Elsa.Logging.Providers;
using Elsa.Logging.Services;
using Elsa.Logging.UI;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Logging.Features;

/// <summary>
/// A feature that installs Logging services for Elsa.
/// </summary>
public class LoggingFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.AddActivity<Log>();
    }

    public LoggingFeature AddLogSinkType<T>() where T : ILogSink
    {
        return AddLogSinkType(typeof(T));
    }

    public LoggingFeature AddLogSinkType(Type sinkType)
    {
        Services.Configure<LoggingOptions>(options => options.SinkTypes.Add(sinkType));
        return this;
    }
    
    public LoggingFeature AddLogSink(ILogSink sink)
    {
        Services.Configure<LoggingOptions>(options => options.Sinks.Add(sink));
        return this;
    }

    public override void Apply()
    {
        Services
            .AddScoped<ILogSinkProvider, ConfigurationLogSinkProvider>()
            .AddScoped<ILogSinkProvider, StaticLogSinkProvider>()
            //.AddScoped<ILogSinkFactory, ConsoleSinkFactory>()
            //.AddScoped<ILogSinkFactory, SerilogFileSinkFactory>()
            .AddScoped<ILogSinkRouter, LogSinkRouter>()
            .AddScoped<ILogSinkCatalog, LogSinkCatalog>()
            .AddScoped<IPropertyUIHandler, LogSinkCheckListUIHintHandler>();
    }
}