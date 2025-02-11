using System.Reflection;
using Elsa.Connections.Attributes;
using Elsa.Connections.Contracts;
using Elsa.Connections.Filters;
using Elsa.Connections.ServiceProvider;
using Elsa.Connections.Services;
using Elsa.Connections.UIHints;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Connections.Middleware;
using Elsa.Connections.Models;

namespace Elsa.Connections.Features;

/// <summary>
/// A feature that installs API endpoints to interact with skilled agents.
/// </summary>
[DependsOn(typeof(WorkflowsFeature))]
[UsedImplicitly]
public class ConnectionsFeatures(IModule module) : FeatureBase(module)
{

    /// <summary>
    /// A set of connection types to make available to the system. 
    /// </summary>
    public HashSet<Type> ConnectionTypes { get; } = [];

    /// <inheritdoc />
    public override void Apply()
    {
        // Obfuscate Connection Properties.
        Services.AddActivityStateFilter<PropertyAttributeFilter>();
        // Activity property options providers.
        Services.AddScoped<Elsa.Workflows.IPropertyUIHandler, ConnectionOptionsProvider>();

        Services.AddSingleton<IConnectionDescriptorRegistry, ConnectionRegistry>();

        //UIHints
        Services.AddScoped<IUIHintHandler, ConnexionDropDownUIHintHandler>();

        Services.Configure<ConnectionOptions>(options =>
        {
            foreach (var connectionType in ConnectionTypes.Distinct())
                options.ConnectionTypes.Add(connectionType);
        });
    }
    public override void Configure()
    {
        //TODO: Need to insert the middleware just before the BackgroundActivityInvoker
        //How to be sure that it is inserted before?
        var workflowFeature = Module.Configure<WorkflowsFeature>()
            .WithDefaultActivityExecutionPipeline(pipeline => pipeline.Insert<ConnectionMiddleware>(3));
        base.Configure();
    }
    public ConnectionsFeatures AddConnectionsFrom<TMarker>()
    {
        var connectionTypes = typeof(TMarker).Assembly.GetExportedTypes()
           .Where(x => x.GetCustomAttribute<ConnectionPropertyAttribute>() != null )
           .ToList();

        ConnectionTypes.AddRange(connectionTypes);

        return this;
    }

}
