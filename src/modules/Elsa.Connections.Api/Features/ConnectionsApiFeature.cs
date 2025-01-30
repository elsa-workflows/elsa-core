using Elsa.Connections.Api.Services;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Api.Features;

/// <summary>
/// A feature that installs API endpoints to interact connections.
/// </summary>
[DependsOn(typeof(ConnectionPersistenceFeature))]
[UsedImplicitly]
public class ConnectionsApiFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<ConnectionsApiFeature>();
    }

    public override void Apply()
    {
        
        Services.AddSingleton<IConnectionRepository, InMemoryConnectionRepository>();

        Services
              
              .AddMemoryStore<ConnectionConfigurationMetadataModel, InMemoryConnectionRepository>();

    }
}