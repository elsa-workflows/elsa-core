using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Persistence.EFCore.Features;
using Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Credentials.Workflows.Features;
using Elsa.Connections.Credentials.Workflows.Services;
using Elsa.Connections.Services;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeOutboundGrantTests(PostgreSqlConnectionsFixture fixture)
{
    [Fact]
    public async Task RealAdmittedWorkflowReceivesNoExactInstanceCredentialUseGrant()
    {
        await fixture.ResetSchemaAsync();
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString) { Outcome = "suspended" };
        await using var services = AdmissionRuntimeHost.CreateServices(fixture.ConnectionString, probe, configureBeforeAdmission: registrations =>
        {
            // Register the existing grant/binding modules in the SAME execution host. There is
            // no new credential store, another executor, secret value or remote provider here.
            var module = registrations.CreateModule();
            module.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = AdmissionWorkerHost.EnvironmentId);
            module.Configure<WorkflowCredentialUseGrantsFeature>();
            module.Configure<EFCoreConnectionsPersistenceFeature>(feature => feature.UsePostgreSql(fixture.ConnectionString));
            module.Apply();
        });
        using var tenant = AdmissionRuntimeHost.EnterTenant(services);
        await AdmissionRuntimeHost.MigrateAsync(services);
        await using (var connections = await services.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync())
        {
            await connections.Database.MigrateAsync();
        }
        await AdmissionRuntimeHost.BootstrapAsync(services);
        var execution = services.GetRequiredService<AdmissionExecutionService>();
        var admitted = await execution.AdmitAsync(AdmissionWorkerHost.Event());
        Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
        var result = (await execution.ExecuteAsync(admitted.AdmissionId!))!;
        Assert.Equal(WorkflowSubStatus.Suspended, result.SubStatus);
        var context = probe.PreparedContext!;
        Assert.Equal(result.WorkflowInstanceId, context.Id);
        var bindings = services.GetRequiredService<IConnectionCredentialBindingStore>();
        var binding = await bindings.TryCreateAsync(AdmissionWorkerHost.TenantId, AdmissionWorkerHost.EnvironmentId, "fixture-logical-binding", "fixture-connection");
        Assert.NotNull(binding);
        var grants = services.GetRequiredService<IConnectionCredentialUseGrantStore>();
        Assert.Null(await grants.FindAsync(AdmissionWorkerHost.TenantId, AdmissionWorkerHost.EnvironmentId, context.Id, binding.LogicalBindingId));
        var policy = services.GetRequiredService<StoredConnectionCredentialBindingUseAuthorizer>();
        Assert.False(await policy.AuthorizeAsync(new(AdmissionWorkerHost.TenantId, AdmissionWorkerHost.EnvironmentId,
            binding.LogicalBindingId, binding.ConnectionId, binding.Revision, context.Id)));
        var resolver = Assert.IsType<WorkflowCredentialResolver>(services.GetRequiredService<IWorkflowCredentialResolver>());
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => resolver.ResolveAsync(context, binding.LogicalBindingId));
        // This first-host background facade throws a different exception. The exact resolver
        // denial above discriminates the missing-grant check from reaching that later facade.
        var background = services.GetRequiredService<IConnectionBackgroundUseService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => background.ResolveForUseAsync(AdmissionWorkerHost.TenantId,
            AdmissionWorkerHost.EnvironmentId, binding.ConnectionId));
        Assert.Null(await grants.FindAsync(AdmissionWorkerHost.TenantId, AdmissionWorkerHost.EnvironmentId, context.Id, binding.LogicalBindingId));
        Assert.Equal(1, probe.Count("activityEffects"));
        await AdmissionProofObservation.WriteAsync(fixture, "runtime-outbound-no-instance-grant",
            GetType().FullName + "." + nameof(RealAdmittedWorkflowReceivesNoExactInstanceCredentialUseGrant), "default", [],
            new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true },
            new Dictionary<string, object> { ["realWorkflowExecuted"] = true, ["bindingExists"] = true, ["instanceGrantAbsent"] = true,
                ["storedGrantPolicyDenied"] = true, ["resolverDeniedBeforeBackground"] = true, ["credentialReturned"] = false });
    }
}
