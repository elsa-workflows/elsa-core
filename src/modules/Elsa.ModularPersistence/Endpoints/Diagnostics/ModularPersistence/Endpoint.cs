using Elsa.Abstractions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Diagnostics;
using Elsa.ModularPersistence.Permissions;
using JetBrains.Annotations;

namespace Elsa.ModularPersistence.Endpoints.Diagnostics.ModularPersistence;

[PublicAPI]
internal sealed class Endpoint(IModularPersistenceDiagnosticsService diagnosticsService) : ElsaEndpointWithoutRequest<ModularPersistenceDiagnostics>
{
    public override void Configure()
    {
        Get("/diagnostics/modular-persistence");
        ConfigurePermissions(ModularPersistencePermissions.ReadDiagnostics);
    }

    public override Task<ModularPersistenceDiagnostics> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(diagnosticsService.GetDiagnostics());
    }
}
