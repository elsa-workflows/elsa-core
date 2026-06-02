using Elsa.Abstractions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions.Audit;

internal sealed class Endpoint(IRuntimeSchemaAuditTrail auditTrail) : ElsaEndpointWithoutRequest<RuntimeSchemaAuditResponse>
{
    public override void Configure()
    {
        Get("/admin/modular-persistence/runtime-storage-definitions/{id}/audit");
        ConfigurePermissions(ModularPersistencePermissions.ReadRuntimeStorageDefinitions);
    }

    public override async Task<RuntimeSchemaAuditResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var entries = await auditTrail.ListAsync(Route<string>("id"), cancellationToken);
        return new RuntimeSchemaAuditResponse(entries);
    }
}
