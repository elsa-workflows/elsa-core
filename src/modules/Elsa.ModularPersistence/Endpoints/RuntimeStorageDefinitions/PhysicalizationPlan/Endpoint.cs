using Elsa.Abstractions;
using Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;
using Elsa.ModularPersistence.Permissions;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions.PhysicalizationPlan;

internal sealed class Endpoint(IRuntimePhysicalizationOperations operations) : ElsaEndpoint<RuntimeStoragePhysicalizationPlanRequest, RuntimeStoragePhysicalizationPlanResponse>
{
    public override void Configure()
    {
        Post("/admin/modular-persistence/runtime-storage-definitions/{id}/physicalization-plan");
        ConfigurePermissions(ModularPersistencePermissions.ReadRuntimeStorageDefinitions);
    }

    public override async Task<RuntimeStoragePhysicalizationPlanResponse> ExecuteAsync(RuntimeStoragePhysicalizationPlanRequest request, CancellationToken cancellationToken)
    {
        var plans = await operations.PlanAsync(Route<string>("id")!, request.ProviderName, cancellationToken);
        return new RuntimeStoragePhysicalizationPlanResponse(plans);
    }
}
