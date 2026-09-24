using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.ClusteredHosting;

public class ActivityRegistrySyncTests(App app) : AppComponentTest(app)
{
    private readonly IServiceScope _pod1Scope = app.Cluster.Pod1.Services.CreateScope();
    private readonly IServiceScope _pod2Scope = app.Cluster.Pod2.Services.CreateScope();
    private readonly IServiceScope _pod3Scope = app.Cluster.Pod3.Services.CreateScope();

    [Fact]
    public async Task ImportWorkflowActivity_ShouldUpdateOtherPods()
    {
        var pod1ActivityRegistry = _pod1Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var pod2ActivityRegistry = _pod2Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var pod3ActivityRegistry = _pod3Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        
        var definitionId = $"RegistrySync{Guid.NewGuid():N}";
        var activityName = definitionId.Pascalize();
        var secondDefinitionId = $"RegistrySyncBulk{Guid.NewGuid():N}";
        var secondActivityName = secondDefinitionId.Pascalize();
        var importer = _pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionImporter>();
        var definitionManager = _pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();

        try
        {
            var firstInsertTenantId = $"RegistrySyncRace{Guid.NewGuid():N}";
            var generationStores = new[]
            {
                _pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRegistryGenerationStore>(),
                _pod2Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRegistryGenerationStore>(),
                _pod3Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRegistryGenerationStore>()
            };
            var firstInsertResults = await Task.WhenAll(generationStores.Select(store => store.IncrementAsync(firstInsertTenantId)));
            Assert.All(firstInsertResults, generation => Assert.InRange(generation, 1, 3));
            Assert.Equal(3, await generationStores[0].GetGenerationAsync(firstInsertTenantId));

            await ImportAsync(importer, definitionId, activityName);
            await WaitUntilAsync(() =>
                pod1ActivityRegistry.Find(activityName, 1) is not null &&
                pod2ActivityRegistry.Find(activityName, 1) is not null &&
                pod3ActivityRegistry.Find(activityName, 1) is not null,
                GetGenerationDiagnostics);

            await ImportAsync(importer, definitionId, activityName);
            await WaitUntilAsync(() =>
                pod1ActivityRegistry.Find(activityName, 2) is not null &&
                pod2ActivityRegistry.Find(activityName, 2) is not null &&
                pod3ActivityRegistry.Find(activityName, 2) is not null);

            await WaitUntilAsync(() =>
                pod1ActivityRegistry.Find(activityName, 1)?.IsBrowsable == false &&
                pod2ActivityRegistry.Find(activityName, 1)?.IsBrowsable == false &&
                pod3ActivityRegistry.Find(activityName, 1)?.IsBrowsable == false);

            await definitionManager.DeleteVersionAsync(definitionId, 2);
            await WaitUntilAsync(() =>
                pod1ActivityRegistry.Find(activityName, 2) is null &&
                pod2ActivityRegistry.Find(activityName, 2) is null &&
                pod3ActivityRegistry.Find(activityName, 2) is null &&
                pod1ActivityRegistry.Find(activityName, 1) is not null &&
                pod2ActivityRegistry.Find(activityName, 1) is not null &&
                pod3ActivityRegistry.Find(activityName, 1) is not null);

            await ImportAsync(importer, secondDefinitionId, secondActivityName);
            await WaitUntilAsync(() =>
                pod1ActivityRegistry.Find(secondActivityName) is not null &&
                pod2ActivityRegistry.Find(secondActivityName) is not null &&
                pod3ActivityRegistry.Find(secondActivityName) is not null);

            await definitionManager.BulkDeleteByDefinitionIdsAsync([definitionId, secondDefinitionId]);
            await WaitUntilAsync(() =>
                pod1ActivityRegistry.Find(activityName) is null &&
                pod2ActivityRegistry.Find(activityName) is null &&
                pod3ActivityRegistry.Find(activityName) is null &&
                pod1ActivityRegistry.Find(secondActivityName) is null &&
                pod2ActivityRegistry.Find(secondActivityName) is null &&
                pod3ActivityRegistry.Find(secondActivityName) is null);
        }
        finally
        {
            await definitionManager.BulkDeleteByDefinitionIdsAsync([definitionId, secondDefinitionId]);
        }
    }

    private static async Task ImportAsync(IWorkflowDefinitionImporter importer, string definitionId, string activityName)
    {
        var result = await importer.ImportAsync(new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Name = activityName,
                DefinitionId = definitionId,
                Options = new WorkflowOptions
                {
                    UsableAsActivity = true,
                    AutoUpdateConsumingWorkflows = true
                }
            },
            Publish = true
        });

        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.ValidationErrors.Select(x => x.Message)));
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        await WaitUntilAsync(predicate, () => Task.FromResult(string.Empty));
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, Func<Task<string>> diagnostics)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (!predicate() && DateTimeOffset.UtcNow < deadline)
            await Task.Delay(100);

        Assert.True(predicate(), $"The workflow-as-activity registry did not converge on all pods. {await diagnostics()}");
    }

    private async Task<string> GetGenerationDiagnostics()
    {
        var stores = new[]
        {
            _pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRegistryGenerationStore>(),
            _pod2Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRegistryGenerationStore>(),
            _pod3Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRegistryGenerationStore>()
        };
        var values = await Task.WhenAll(stores.Select(async store => new
        {
            Default = await store.GetGenerationAsync(string.Empty),
            Tenant1 = await store.GetGenerationAsync("Tenant1"),
            Agnostic = await store.GetGenerationAsync("*"),
        }));
        var scopes = new[] { _pod1Scope, _pod2Scope, _pod3Scope };
        var podStates = scopes.Select((scope, index) =>
        {
            var tenantId = scope.ServiceProvider.GetRequiredService<ITenantAccessor>().TenantId;
            var registry = scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
            var descriptors = registry.ListByProvider(typeof(WorkflowDefinitionActivityProvider))
                .Select(x => $"{x.Name}/{x.TenantId}/v{x.Version}");
            return $"pod{index + 1} tenant={tenantId} descriptors=[{string.Join(",", descriptors)}]";
        });
        return "Generations: " + string.Join("; ", values.Select(x => $"default={x.Default}, Tenant1={x.Tenant1}, agnostic={x.Agnostic}")) + "; " + string.Join("; ", podStates);
    }

    protected override void OnDispose()
    {
        _pod1Scope.Dispose();
        _pod2Scope.Dispose();
        _pod3Scope.Dispose();
    }
}
