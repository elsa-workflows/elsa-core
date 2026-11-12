using Elsa.Common;
using Elsa.Common.Features;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Mediator.HostedServices;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Tenants.Extensions;
using Elsa.Tenants.Mediator.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Tasks;
using Medallion.Threading.FileSystem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// Recovers a tenant-agnostic ("*") instance whose host died mid-burst, on a second host that shares the SQLite
/// database and serves the tenants "", "*" and "tenant-a". The dead burst committed the activity it was executing,
/// so recovery replaces that activity's "*" execution record, which the EF store allows only under the "*" tenant.
/// </summary>
public class AgnosticInstanceRecoveryTests(ITestOutputHelper testOutputHelper)
{
    private static readonly string[] TenantIds = [Tenant.DefaultTenantId, Tenant.AgnosticTenantId, "tenant-a"];
    private static readonly TimeSpan RecoveringHostInactivityThreshold = TimeSpan.FromSeconds(1);

    [Theory(DisplayName = "An interrupted tenant-agnostic instance is recovered under the agnostic tenant, whatever tenant runs the recovery")]
    [InlineData(nameof(RestartInterruptedWorkflowsTask))]
    [InlineData(nameof(IInterruptedRecoveryScanner))]
    public async Task RecoversAgnosticInstanceUnderAgnosticTenant(string recoveryPath)
    {
        var tempDirectory = Directory.CreateTempSubdirectory("elsa-agnostic-recovery-");
        var connectionString = $"Data Source={Path.Join(tempDirectory.FullName, "elsa.db")};Default Timeout=30;Pooling=False";
        AgnosticRecoveryGate.Current = new();
        Task? deadBurst = null;

        try
        {
            // the dead host: runs the "*" instance until it hangs in an activity it committed as executing
            await using var deadHost = CreateHost(connectionString, Path.Join(tempDirectory.FullName, "locks-dead"));
            await StartHostAsync(deadHost);
            string instanceId;

            using (PushTenant(deadHost, await FindTenantAsync(deadHost, Tenant.AgnosticTenantId)))
            {
                var client = await deadHost.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
                var created = await client.CreateInstanceAsync(new()
                {
                    WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(AgnosticRecoveryWorkflow), VersionOptions.Published)
                });
                Assert.False(created.CannotStart);
                instanceId = client.WorkflowInstanceId;
                deadBurst = Task.Run(() => client.RunInstanceAsync(RunWorkflowInstanceRequest.Empty));
            }

            await AgnosticRecoveryGate.Current.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Contains(await ListActivityExecutionRecordTenantIdsAsync(deadHost, instanceId), tenantId => tenantId == Tenant.AgnosticTenantId);

            // the recovering host: runs the recovery under the default tenant, as the host-scoped scheduler and the startup scan do
            await using var recoveringHost = CreateHost(connectionString, Path.Join(tempDirectory.FullName, "locks-recovering"), RecoveringHostInactivityThreshold);
            await StartHostAsync(recoveringHost);
            await Task.Delay(RecoveringHostInactivityThreshold);
            await using var scope = recoveringHost.CreateAsyncScope();
            var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();

            if (recoveryPath == nameof(IInterruptedRecoveryScanner))
            {
                // as a graceful drain leaves it
                using (PushTenant(recoveringHost, await FindTenantAsync(recoveringHost, Tenant.AgnosticTenantId)))
                    Assert.True(await instanceStore.TryMarkInterruptedAsync(instanceId));
            }

            using (PushTenant(recoveringHost, await FindTenantAsync(recoveringHost, Tenant.DefaultTenantId)))
            {
                if (recoveryPath == nameof(IInterruptedRecoveryScanner))
                    Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<IInterruptedRecoveryScanner>().ScanAndRequeueAsync(CancellationToken.None));
                else
                    await ActivatorUtilities.CreateInstance<RestartInterruptedWorkflowsTask>(scope.ServiceProvider).ExecuteAsync(CancellationToken.None);
            }

            var recovered = await WaitForFinishedAsync(instanceStore, instanceId, TimeSpan.FromSeconds(15));
            Assert.Equal(WorkflowSubStatus.Finished, recovered.SubStatus);
            Assert.Equal(Tenant.AgnosticTenantId, recovered.TenantId);
            Assert.Equal(2, AgnosticRecoveryGate.Current.ExecutionCount);
            Assert.All(await ListActivityExecutionRecordTenantIdsAsync(recoveringHost, instanceId), tenantId => Assert.Equal(Tenant.AgnosticTenantId, tenantId));
        }
        finally
        {
            AgnosticRecoveryGate.Current.Continue.TrySetResult();

            if (deadBurst != null)
            {
                try { await deadBurst.WaitAsync(TimeSpan.FromSeconds(10)); }
                catch (Exception ex) when (!ex.IsFatal()) { /* the dead burst may fail once released; it only has to stop */ }
            }

            try { tempDirectory.Delete(true); }
            catch (IOException) { /* best effort */ }
        }
    }

    private ServiceProvider CreateHost(string connectionString, string lockDirectory, TimeSpan? inactivityThreshold = null)
    {
        return (ServiceProvider)new TestApplicationBuilder(testOutputHelper)
            .AddActivitiesFrom<AgnosticInstanceRecoveryTests>()
            .AddWorkflow<AgnosticRecoveryWorkflow>()
            .ConfigureServices(services => services
                // commit before each activity, so the activity a burst dies in has an execution record
                .Configure<CommitStateOptions>(options => options.DefaultWorkflowCommitStrategy = new ActivityExecutingWorkflowStrategy())
                // only the recovering host: the threshold also paces the heartbeat, which would keep the dead burst fresh
                .Configure<RuntimeOptions>(options => options.InactivityThreshold = inactivityThreshold ?? options.InactivityThreshold))
            .ConfigureElsa(elsa => elsa.Configure<MultitenancyFeature>(feature => feature.UseTenantsProvider(_ => new FixedTenantsProvider(TenantIds))))
            .ConfigureElsa(elsa => elsa
                .UseTenants()
                .UseWorkflowManagement(management => management.UseEntityFrameworkCore(persistence => persistence.UseSqlite(connectionString)))
                .UseWorkflowRuntime(runtime =>
                {
                    runtime.UseEntityFrameworkCore(persistence => persistence.UseSqlite(connectionString));
                    runtime.DistributedLockProvider = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(lockDirectory));
                }))
            .Build();
    }

    private static async Task StartHostAsync(IServiceProvider host)
    {
        var hostedServices = host.GetServices<IHostedService>().ToList();
        await hostedServices.OfType<SetupMediatorPipelines>().Single().StartAsync(CancellationToken.None);
        await hostedServices.OfType<BackgroundCommandSenderHostedService>().Single().StartAsync(CancellationToken.None);
        await host.GetRequiredService<ITenantService>().ActivateTenantsAsync();
    }

    private static async Task<Tenant> FindTenantAsync(IServiceProvider host, string tenantId)
    {
        var tenant = await host.GetRequiredService<ITenantService>().FindAsync(tenantId);
        Assert.NotNull(tenant);
        return tenant;
    }

    private static IDisposable PushTenant(IServiceProvider host, Tenant tenant) => host.GetRequiredService<ITenantAccessor>().PushContext(tenant);

    private static async Task<List<string?>> ListActivityExecutionRecordTenantIdsAsync(IServiceProvider host, string instanceId)
    {
        await using var dbContext = await host.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync();
        return await dbContext.ActivityExecutionRecords.IgnoreQueryFilters().Where(x => x.WorkflowInstanceId == instanceId).Select(x => x.TenantId).ToListAsync();
    }

    private static async Task<Management.Entities.WorkflowInstance> WaitForFinishedAsync(IWorkflowInstanceStore instanceStore, string instanceId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var filter = new WorkflowInstanceFilter { Id = instanceId, TenantAgnostic = true };

        while (true)
        {
            var instance = await instanceStore.FindAsync(filter);

            if (instance is { Status: WorkflowStatus.Finished })
                return instance;

            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Instance {instanceId} did not finish within {timeout}; it is {instance?.Status}/{instance?.SubStatus} under tenant '{instance?.TenantId}'.");

            await Task.Delay(50);
        }
    }

    private sealed class FixedTenantsProvider(IEnumerable<string> tenantIds) : ITenantsProvider
    {
        public Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(tenantIds.Select(id => new Tenant { Id = id, Name = id }));

        public async Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default) =>
            filter.Apply((await ListAsync(cancellationToken)).AsQueryable()).FirstOrDefault();
    }
}

/// <summary>Gate for <see cref="AgnosticRecoveryHangActivity"/>, so the test can let the first burst hang as if its host had died.</summary>
public sealed class AgnosticRecoveryGate
{
    public static AgnosticRecoveryGate Current { get; set; } = new();

    public int ExecutionCount;
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>Hangs on its first execution, which stands in for the dead host's burst, and completes on any later one.</summary>
public class AgnosticRecoveryHangActivity : CodeActivity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (Interlocked.Increment(ref AgnosticRecoveryGate.Current.ExecutionCount) != 1)
            return;

        AgnosticRecoveryGate.Current.Started.TrySetResult();
        await AgnosticRecoveryGate.Current.Continue.Task;
    }
}

/// <summary>A CLR workflow, so its definition is tenant-agnostic.</summary>
public class AgnosticRecoveryWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("first"),
                new AgnosticRecoveryHangActivity(),
                new WriteLine("second")
            }
        };
    }
}
