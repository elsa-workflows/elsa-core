using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Options;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

/// <summary>One actual isolated runtime host and its trusted synthetic subscription per scenario.</summary>
internal sealed class AdmissionRuntimeTestFixture(PostgreSqlConnectionsFixture fixture)
{
    public static async Task MaterializeAsync(AdmissionRuntimeScenario host)
    {
        var record = (await host.Store.FindAsync(host.AdmissionId))!;
        var binding = host.Services.GetRequiredService<AdmissionRuntimeBinding>();
        var definitions = host.Services.GetRequiredService<IWorkflowDefinitionService>();
        var definition = (await definitions.FindWorkflowDefinitionAsync(binding.Artifact.Id))!;
        var graph = await definitions.MaterializeWorkflowAsync(definition);
        var instanceId = Guid.NewGuid().ToString("N");
        record = (await host.Store.BeginCreationAsync(record.Id, record.Revision, instanceId))!;
        var instances = host.Services.GetRequiredService<IWorkflowInstanceManager>();
        var instance = instances.CreateWorkflowInstance(graph.Workflow, new WorkflowInstanceOptions
        {
            WorkflowInstanceId = instanceId,
            Input = new Dictionary<string, object>
            {
                ["Event"] = record.Payload!, ["ProviderEventId"] = record.ProviderEventId!, ["ChannelId"] = binding.Configuration.ChannelId
            }
        });
        await instances.CreateAsync(instance);
        var persisted = (await instances.FindByIdAsync(instanceId))!;
        var serializer = host.Services.GetRequiredService<IWorkflowStateSerializer>();
        Assert.Equal(serializer.Serialize(instance.WorkflowState), serializer.Serialize(persisted.WorkflowState));
        var fingerprint = AdmissionHash.Compute(serializer.Serialize(persisted.WorkflowState));
        Assert.NotNull(await host.Store.CompleteCreationAsync(record.Id, record.Revision, fingerprint));
    }

    public async Task RunAsync(Func<AdmissionRuntimeScenario, Task> assertion, IInterceptor? interceptor = null, bool shell = false)
    {
        await RunUnprovisionedAsync(async host =>
        {
            await AdmissionRuntimeHost.BootstrapAsync(host.Services);
            var execution = host.Services.GetRequiredService<AdmissionExecutionService>();
            var admission = await execution.AdmitAsync(AdmissionWorkerHost.Event());
            Assert.Equal(AdmissionOutcome.Committed, admission.Outcome);
            await assertion(new(host.Services, host.Probe, execution, host.Services.GetRequiredService<IAdmissionStore>(), admission.AdmissionId!));
        }, interceptor, shell);
    }

    public async Task RunUnprovisionedAsync(Func<AdmissionUnprovisionedRuntimeScenario, Task> assertion, IInterceptor? interceptor = null, bool shell = false, bool canStartWorkflow = false, bool unallowlistedActivity = false)
    {
        await fixture.ResetSchemaAsync();
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString);
        await using var services = AdmissionRuntimeHost.CreateServices(fixture.ConnectionString, probe, shell: shell, admissionInterceptor: interceptor, canStartWorkflow: canStartWorkflow, unallowlistedActivity: unallowlistedActivity);
        using var tenant = AdmissionRuntimeHost.EnterTenant(services);
        await AdmissionRuntimeHost.MigrateAsync(services);
        await assertion(new(services, probe));
    }
}

internal sealed record AdmissionRuntimeScenario(IServiceProvider Services, AdmissionRuntimeProbe Probe,
    AdmissionExecutionService Execution, IAdmissionStore Store, string AdmissionId);

internal sealed record AdmissionUnprovisionedRuntimeScenario(IServiceProvider Services, AdmissionRuntimeProbe Probe);
