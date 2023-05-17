using Elsa.Abstractions;
using Elsa.Environments.Contracts;
using Elsa.Environments.Models;
using JetBrains.Annotations;

namespace Elsa.Environments.Endpoints.Environments.List;

[PublicAPI]
internal class Endpoint : ElsaEndpointWithoutRequest<Response>
{
    private readonly IEnvironmentsManager _environmentsManager;

    public Endpoint(IEnvironmentsManager environmentsManager)
    {
        _environmentsManager = environmentsManager;
    }
    
    public override void Configure()
    {
        Get("/environments");
        ConfigurePermissions("read:environments");
    }

    public override async Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var environments = await _environmentsManager.ListEnvironmentsAsync(cancellationToken);
        var defaultEnvironmentName = await _environmentsManager.GetDefaultEnvironmentNameAsync(cancellationToken);
        return new(environments.ToList(), defaultEnvironmentName);
    }
}

[PublicAPI]
internal record Response(ICollection<ServerEnvironment> Environments, string? DefaultEnvironmentName = default);