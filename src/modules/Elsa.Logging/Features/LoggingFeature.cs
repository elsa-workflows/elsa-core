using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Logging.Activities;
using Elsa.Logging.Contracts;
using Elsa.Logging.HostedServices;
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
    
    public LoggingFeature AddLogSink(ILogSink sink)
    {
        Services.Configure<LoggingOptions>(options => options.Sinks.Add(sink));
        return this;
    }

    public LoggingFeature ConfigureDefaults(params string[] defaults)
    {
        ConfigureDefaults(options => options.Defaults = [..defaults]);
        return this;   
    }
    
    public LoggingFeature ConfigureDefaults(Action<LoggingOptions> configure)
    {
        Services.Configure(configure);
        return this;   
    }

    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<LogEntryBackgroundWorker>();
    }

    public override void Apply()
    {
        Services
            .AddScoped<ILogSinkProvider, ConfigurationLogSinkProvider>()
            .AddScoped<ILogSinkProvider, StaticLogSinkProvider>()
            .AddScoped<ILogSinkRouter, LogSinkRouter>()
            .AddScoped<IPropertyUIHandler, LogSinkCheckListUIHintHandler>()
            .AddSingleton<ILogEntryQueue, LogEntryQueue>()
            .AddSingleton<ILogSinkCatalog, LogSinkCatalog>();
    }
}