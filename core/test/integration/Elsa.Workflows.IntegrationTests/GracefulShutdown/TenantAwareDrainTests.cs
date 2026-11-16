using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// A drain whose deadline passes while a workflow is mid-activity, in a named tenant, a tenant with its own database and
/// the default tenant. The drain runs outside any tenant, as it does on a real host stop.
/// </summary>
public class TenantAwareDrainTests : IAsyncDisposable
{
    private const string NamedTenantId = "tenant-a";
    private const string OwnDatabaseTenantId = "tenant-own";

    private readonly IServiceProvider _services;
    private readonly MemoryWorkflowInstanceStore _ownDatabaseStore = new(new MemoryStore<WorkflowInstance>(), new DefaultTenantAccessor());
    private readonly HangGate _gate = new();

    public TenantAwareDrainTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .AddActivitiesFrom<TenantAwareDrainTests>()
            // Commit before each activity, so the instance row exists while the activity hangs.
            .ConfigureServices(services => services.Configure<CommitStateOptions>(options => options.DefaultWorkflowCommitStrategy = new ActivityExecutingWorkflowStrategy()))
            .ConfigureElsa(elsa => elsa
                .UseWorkflowManagement(management => management.UseWorkflowInstances(instances => instances.WorkflowInstanceStore = sp =>
                    HasOwnDatabase(sp.GetRequiredService<ITenantAccessor>().Tenant) ? _ownDatabaseStore : sp.GetRequiredService<MemoryWorkflowInstanceStore>()))
                .UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(options =>
                {
                    options.DrainDeadline = TimeSpan.FromMilliseconds(50);
                    options.IngressPauseTimeout = TimeSpan.FromMilliseconds(50);
                })))
            .Build();
    }

    [Theory(DisplayName = "A force-cancelling drain marks the instance Interrupted in its own tenant instead of reporting it unpersisted")]
    [InlineData(NamedTenantId)]
    [InlineData(OwnDatabaseTenantId)]
    [InlineData(Tenant.DefaultTenantId)]
    public async Task ForceCancelMarksInstanceInterruptedInItsTenant(string tenantId)
    {
        await _services.PopulateRegistriesAsync();
        var tenant = CreateTenant(tenantId);
        var run = RunInTenantAsync(tenant, new TestWorkflow(builder => builder.Root = new HangingActivity { Gate = _gate }));
        await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var outcome = await _services.GetRequiredService<IDrainOrchestrator>().DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.DeadlineExceeded, outcome.OverallResult);
        var instanceId = Assert.Single(outcome.ForceCancelledInstanceIds);
        var instance = await FindAsync(tenant, instanceId);
        var reasons = string.Join(", ", await ListInterruptedReasonsAsync(instanceId));
        Assert.Equal(
            (WorkflowStatus.Running, WorkflowSubStatus.Interrupted, tenantId, WorkflowInterruptedPayload.ReasonDeadlineBreach),
            (instance?.Status, instance?.SubStatus, instance?.TenantId, reasons));

        _gate.Release.TrySetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    public async ValueTask DisposeAsync()
    {
        _gate.Release.TrySetResult();

        if (_services is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }

    // Runs on its own task, so the tenant it pushes does not reach the drain.
    private Task RunInTenantAsync(Tenant tenant, IWorkflow workflow) => Task.Run(async () =>
    {
        await using var scope = _services.GetRequiredService<ITenantScopeFactory>().CreateScope(tenant);
        await scope.ServiceProvider.GetRequiredService<IWorkflowRunner>().RunAsync(workflow);
    });

    private async Task<WorkflowInstance?> FindAsync(Tenant tenant, string instanceId)
    {
        await using var scope = _services.GetRequiredService<ITenantScopeFactory>().CreateScope(tenant);
        return await scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter { Id = instanceId });
    }

    private async Task<string[]> ListInterruptedReasonsAsync(string instanceId)
    {
        using var scope = _services.CreateScope();
        var records = await scope.ServiceProvider.GetRequiredService<IWorkflowExecutionLogStore>().FindManyAsync(
            new WorkflowExecutionLogRecordFilter { WorkflowInstanceId = instanceId, EventName = WorkflowInterruptedPayload.WorkflowInterruptedEventName },
            PageArgs.All);
        return records.Items.Select(x => ((WorkflowInterruptedPayload)x.Payload!).Reason).ToArray();
    }

    private static Tenant CreateTenant(string tenantId)
    {
        var connectionStrings = tenantId == OwnDatabaseTenantId ? new Dictionary<string, string?> { ["ConnectionStrings:Default"] = "Data Source=own.db" } : [];
        return new() { Id = tenantId, Name = tenantId, Configuration = new ConfigurationBuilder().AddInMemoryCollection(connectionStrings).Build() };
    }

    private static bool HasOwnDatabase(Tenant? tenant) => tenant?.Configuration["ConnectionStrings:Default"] is not null;

    public sealed class HangGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Hangs until released and ignores cancellation, so the drain's settle wait times out while the cycle is still active.</summary>
    public class HangingActivity : CodeActivity
    {
        public HangGate? Gate { get; set; }

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            Gate!.Started.TrySetResult();
            await Gate.Release.Task;
        }
    }
}
