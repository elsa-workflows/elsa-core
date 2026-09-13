using System.Data.Common;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Elsa.Common.Multitenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Persistence.ConformanceTests;

/// <summary>
/// EF Alterations Save/SaveMany must refuse ID collisions atomically (no pre-read guard).
/// Kept out of the Memory/EF conformance matrix so that suite stays on read/stamp/query/round-trip.
/// </summary>
[Collection(AlterationStoreSqliteConformanceCollection.Name)]
public sealed class EFCoreAlterationStoreTenantOwnershipTests
{
    [Fact]
    public async Task SaveAsync_WhenOtherNamedTenantOwnsPlanId_ThrowsAndLeavesOwnerAndPayload()
    {
        await using var owner = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await owner.Plans.SaveAsync(Plan("shared", "tenant-a", AlterationPlanStatus.Running, "original"));

        using (owner.UseTenant("tenant-b"))
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                owner.Plans.SaveAsync(Plan("shared", "tenant-b", AlterationPlanStatus.Completed, "stolen")));
            Assert.Contains("shared", ex.Message);
        }

        var remaining = await owner.Plans.FindAsync(new AlterationPlanFilter { Id = "shared" });
        AssertUnchangedPlan(remaining, "tenant-a", AlterationPlanStatus.Running, "original");
    }

    [Fact]
    public async Task SaveAsync_WhenAgnosticPlanExists_NamedTenantThrowsAndLeavesOwnerAndPayload()
    {
        await using var scenario = await AlterationStoreScenario.CreateSqliteAsync(Tenant.AgnosticTenantId);
        await scenario.Plans.SaveAsync(Plan("shared", Tenant.AgnosticTenantId, AlterationPlanStatus.Running, "original"));

        using (scenario.UseTenant("tenant-b"))
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Plans.SaveAsync(Plan("shared", "tenant-b", AlterationPlanStatus.Completed, "stolen")));
            Assert.Contains("shared", ex.Message);
        }

        using (scenario.UseTenant(Tenant.AgnosticTenantId))
        {
            var remaining = await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "shared" });
            AssertUnchangedPlan(remaining, Tenant.AgnosticTenantId, AlterationPlanStatus.Running, "original");
        }
    }

    [Fact]
    public async Task SaveAsync_WhenAmbientForgesOwnerTenantIdOnPlan_ThrowsAndLeavesOwnerAndPayload()
    {
        await using var owner = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await owner.Plans.SaveAsync(Plan("shared", "tenant-a", AlterationPlanStatus.Running, "original"));

        using (owner.UseTenant("tenant-b"))
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                owner.Plans.SaveAsync(Plan("shared", "tenant-a", AlterationPlanStatus.Completed, "stolen")));
            Assert.Contains("shared", ex.Message);
        }

        var remaining = await owner.Plans.FindAsync(new AlterationPlanFilter { Id = "shared" });
        AssertUnchangedPlan(remaining, "tenant-a", AlterationPlanStatus.Running, "original");
    }

    [Fact]
    public async Task SaveAsync_WhenSameTenantOwnsPlanId_UpdatesPayload()
    {
        await using var scenario = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await scenario.Plans.SaveAsync(Plan("plan-a", "tenant-a", AlterationPlanStatus.Pending, "before"));

        await scenario.Plans.SaveAsync(Plan("plan-a", "tenant-a", AlterationPlanStatus.Completed, "after"));

        var found = await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" });
        AssertUnchangedPlan(found, "tenant-a", AlterationPlanStatus.Completed, "after");
    }

    [Fact]
    public async Task SaveAsync_WhenIncomingPlanTenantDiffers_PreservesExistingTenantId()
    {
        await using var scenario = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await scenario.Plans.SaveAsync(Plan("plan-a", "tenant-a", AlterationPlanStatus.Pending, "before"));

        await scenario.Plans.SaveAsync(Plan("plan-a", "tenant-b", AlterationPlanStatus.Completed, "after"));

        var found = await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" });
        AssertUnchangedPlan(found, "tenant-a", AlterationPlanStatus.Completed, "after");
    }

    [Fact]
    public async Task SaveAsync_WhenAmbientIsAgnostic_UpdatesAgnosticPlan()
    {
        await using var scenario = await AlterationStoreScenario.CreateSqliteAsync(Tenant.AgnosticTenantId);
        await scenario.Plans.SaveAsync(Plan("shared", Tenant.AgnosticTenantId, AlterationPlanStatus.Pending, "before"));

        await scenario.Plans.SaveAsync(Plan("shared", Tenant.AgnosticTenantId, AlterationPlanStatus.Completed, "after"));

        var found = await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "shared" });
        AssertUnchangedPlan(found, Tenant.AgnosticTenantId, AlterationPlanStatus.Completed, "after");
    }

    [Fact]
    public async Task SaveAsync_WhenOtherNamedTenantOwnsJobId_ThrowsAndLeavesOwnerAndPayload()
    {
        await using var owner = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await owner.Jobs.SaveAsync(Job("shared", "tenant-a", AlterationJobStatus.Running, "original"));

        using (owner.UseTenant("tenant-b"))
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                owner.Jobs.SaveAsync(Job("shared", "tenant-b", AlterationJobStatus.Completed, "stolen")));
            Assert.Contains("shared", ex.Message);
        }

        var remaining = await owner.Jobs.FindAsync(new AlterationJobFilter { Id = "shared" });
        AssertUnchangedJob(remaining, "tenant-a", AlterationJobStatus.Running, "original");
    }

    [Fact]
    public async Task SaveAsync_WhenAmbientForgesOwnerTenantIdOnJob_ThrowsAndLeavesOwnerAndPayload()
    {
        await using var owner = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await owner.Jobs.SaveAsync(Job("shared", "tenant-a", AlterationJobStatus.Running, "original"));

        using (owner.UseTenant("tenant-b"))
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                owner.Jobs.SaveAsync(Job("shared", "tenant-a", AlterationJobStatus.Completed, "stolen")));
            Assert.Contains("shared", ex.Message);
        }

        var remaining = await owner.Jobs.FindAsync(new AlterationJobFilter { Id = "shared" });
        AssertUnchangedJob(remaining, "tenant-a", AlterationJobStatus.Running, "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenOtherNamedTenantOwnsJobId_ThrowsAndLeavesOwnerAndPayload()
    {
        await using var owner = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await owner.Jobs.SaveAsync(Job("shared", "tenant-a", AlterationJobStatus.Running, "original"));

        using (owner.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                owner.Jobs.SaveManyAsync([Job("shared", "tenant-b", AlterationJobStatus.Completed, "stolen")]));
        }

        var remaining = await owner.Jobs.FindAsync(new AlterationJobFilter { Id = "shared" });
        AssertUnchangedJob(remaining, "tenant-a", AlterationJobStatus.Running, "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenAmbientForgesOwnerTenantIdOnJob_ThrowsAndLeavesOwnerAndPayload()
    {
        await using var owner = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await owner.Jobs.SaveAsync(Job("shared", "tenant-a", AlterationJobStatus.Running, "original"));

        using (owner.UseTenant("tenant-b"))
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                owner.Jobs.SaveManyAsync([Job("shared", "tenant-a", AlterationJobStatus.Completed, "stolen")]));
            Assert.Contains("shared", ex.Message);
        }

        var remaining = await owner.Jobs.FindAsync(new AlterationJobFilter { Id = "shared" });
        AssertUnchangedJob(remaining, "tenant-a", AlterationJobStatus.Running, "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenAgnosticJobExists_NamedTenantThrowsAndLeavesOwnerAndPayload()
    {
        await using var scenario = await AlterationStoreScenario.CreateSqliteAsync(Tenant.AgnosticTenantId);
        await scenario.Jobs.SaveAsync(Job("shared", Tenant.AgnosticTenantId, AlterationJobStatus.Running, "original"));

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Jobs.SaveManyAsync([Job("shared", "tenant-b", AlterationJobStatus.Completed, "stolen")]));
        }

        using (scenario.UseTenant(Tenant.AgnosticTenantId))
        {
            var remaining = await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "shared" });
            AssertUnchangedJob(remaining, Tenant.AgnosticTenantId, AlterationJobStatus.Running, "original");
        }
    }

    [Fact]
    public async Task SaveManyAsync_WhenSameTenantOwnsJobId_UpdatesPayload()
    {
        await using var scenario = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await scenario.Jobs.SaveAsync(Job("job-a", "tenant-a", AlterationJobStatus.Pending, "before"));

        await scenario.Jobs.SaveManyAsync([Job("job-a", "tenant-a", AlterationJobStatus.Completed, "after")]);

        var found = await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-a" });
        AssertUnchangedJob(found, "tenant-a", AlterationJobStatus.Completed, "after");
    }

    [Fact]
    public async Task SaveManyAsync_WhenIncomingJobTenantDiffers_PreservesExistingTenantId()
    {
        await using var scenario = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await scenario.Jobs.SaveAsync(Job("job-a", "tenant-a", AlterationJobStatus.Pending, "before"));

        await scenario.Jobs.SaveManyAsync([Job("job-a", "tenant-b", AlterationJobStatus.Completed, "after")]);

        var found = await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-a" });
        AssertUnchangedJob(found, "tenant-a", AlterationJobStatus.Completed, "after");
    }

    [Fact]
    public async Task SaveAsync_WhenTenancyIsDisabled_UpdatesNamedAndAgnosticPlansThroughLegacyStore()
    {
        await using var scenario = await AlterationStoreScenario.CreateSqliteAsync("tenant-b", tenantsEnabled: false);
        await scenario.Plans.SaveAsync(Plan("named", "tenant-a", AlterationPlanStatus.Pending, "before"));
        await scenario.Plans.SaveAsync(Plan("agnostic", Tenant.AgnosticTenantId, AlterationPlanStatus.Pending, "before"));

        await scenario.Plans.SaveAsync(Plan("named", "tenant-a", AlterationPlanStatus.Completed, "after"));
        await scenario.Plans.SaveAsync(Plan("agnostic", Tenant.AgnosticTenantId, AlterationPlanStatus.Completed, "after"));

        AssertUnchangedPlan(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "named" }), "tenant-a", AlterationPlanStatus.Completed, "after");
        AssertUnchangedPlan(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "agnostic" }), Tenant.AgnosticTenantId, AlterationPlanStatus.Completed, "after");
    }

    [Fact]
    public async Task SaveManyAsync_WhenTenancyIsDisabled_UpdatesNamedAndAgnosticJobsThroughLegacyStore()
    {
        await using var scenario = await AlterationStoreScenario.CreateSqliteAsync("tenant-b", tenantsEnabled: false);
        await scenario.Jobs.SaveAsync(Job("named", "tenant-a", AlterationJobStatus.Pending, "before"));
        await scenario.Jobs.SaveAsync(Job("agnostic", Tenant.AgnosticTenantId, AlterationJobStatus.Pending, "before"));

        await scenario.Jobs.SaveManyAsync(
        [
            Job("named", "tenant-a", AlterationJobStatus.Completed, "after"),
            Job("agnostic", Tenant.AgnosticTenantId, AlterationJobStatus.Completed, "after")
        ]);

        AssertUnchangedJob(await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "named" }), "tenant-a", AlterationJobStatus.Completed, "after");
        AssertUnchangedJob(await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "agnostic" }), Tenant.AgnosticTenantId, AlterationJobStatus.Completed, "after");
    }

    [Fact]
    public async Task SaveAsync_ConcurrentSameTenantPlanId_BothWritersSucceedAndOnePayloadWins()
    {
        var gate = new GateFirstAlterationUpdates("AlterationPlans");
        await using var pair = await AlterationStoreScenario.CreateSqlitePairAsync("tenant-a", "tenant-a", gate);
        gate.Arm();

        var results = await Task.WhenAll(
            Capture(() => pair.First.Plans.SaveAsync(Plan("race", "tenant-a", AlterationPlanStatus.Running, "from-a"))),
            Capture(() => pair.Second.Plans.SaveAsync(Plan("race", "tenant-a", AlterationPlanStatus.Completed, "from-b"))));

        Assert.All(results, Assert.Null);
        Assert.Equal(3, gate.MatchedCommandCount);

        using (pair.First.UseTenant("tenant-a"))
        {
            var found = await pair.First.Plans.FindAsync(new AlterationPlanFilter { Id = "race" });
            Assert.NotNull(found);
            Assert.Equal("tenant-a", found.TenantId);
            Assert.Contains(Payload(found), new[] { "from-a", "from-b" });
        }
    }

    [Fact]
    public async Task SaveManyAsync_WhenBatchCollides_RollsBackEarlierInserts()
    {
        await using var owner = await AlterationStoreScenario.CreateSqliteAsync("tenant-a");
        await owner.Jobs.SaveAsync(Job("owned", "tenant-a", AlterationJobStatus.Running, "original"));

        using (owner.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => owner.Jobs.SaveManyAsync(
            [
                Job("new-from-b", "tenant-b", AlterationJobStatus.Pending, "should-roll-back"),
                Job("owned", "tenant-b", AlterationJobStatus.Completed, "stolen")
            ]));

            Assert.Null(await owner.Jobs.FindAsync(new AlterationJobFilter { Id = "new-from-b" }));
        }

        var remaining = await owner.Jobs.FindAsync(new AlterationJobFilter { Id = "owned" });
        AssertUnchangedJob(remaining, "tenant-a", AlterationJobStatus.Running, "original");
    }

    [Fact]
    public async Task SaveAsync_ConcurrentNamedTenantsOnEmptyPlanId_OneOwnerKeepsPayload()
    {
        var gate = new GateFirstAlterationUpdates("AlterationPlans");
        await using var pair = await AlterationStoreScenario.CreateSqlitePairAsync("tenant-a", "tenant-b", gate);
        gate.Arm();

        var results = await Task.WhenAll(
            Capture(() => pair.First.Plans.SaveAsync(Plan("race", "tenant-a", AlterationPlanStatus.Running, "from-a"))),
            Capture(() => pair.Second.Plans.SaveAsync(Plan("race", "tenant-b", AlterationPlanStatus.Completed, "from-b"))));

        Assert.Equal(1, results.Count(ex => ex is null));
        Assert.Equal(1, results.Count(ex => ex is InvalidOperationException));
        Assert.Equal(3, gate.MatchedCommandCount);

        var winnerIsA = results[0] is null;
        using (pair.First.UseTenant(winnerIsA ? "tenant-a" : "tenant-b"))
        {
            var remaining = await pair.First.Plans.FindAsync(new AlterationPlanFilter { Id = "race" });
            AssertUnchangedPlan(
                remaining,
                winnerIsA ? "tenant-a" : "tenant-b",
                winnerIsA ? AlterationPlanStatus.Running : AlterationPlanStatus.Completed,
                winnerIsA ? "from-a" : "from-b");
        }
    }

    [Fact]
    public async Task SaveAsync_ConcurrentNamedVersusAgnosticOnExistingStarPlan_PreservesStarPayload()
    {
        var gate = new GateFirstAlterationUpdates("AlterationPlans");
        await using var pair = await AlterationStoreScenario.CreateSqlitePairAsync(Tenant.AgnosticTenantId, "tenant-b", gate);
        await pair.First.Plans.SaveAsync(Plan("shared", Tenant.AgnosticTenantId, AlterationPlanStatus.Running, "original"));
        gate.Arm();

        var results = await Task.WhenAll(
            Capture(() => pair.First.Plans.SaveAsync(Plan("shared", Tenant.AgnosticTenantId, AlterationPlanStatus.Failed, "agnostic-update"))),
            Capture(() => pair.Second.Plans.SaveAsync(Plan("shared", "tenant-b", AlterationPlanStatus.Completed, "stolen"))));

        Assert.IsType<InvalidOperationException>(results[1]);
        Assert.Equal(3, gate.MatchedCommandCount);

        using (pair.First.UseTenant(Tenant.AgnosticTenantId))
        {
            var remaining = await pair.First.Plans.FindAsync(new AlterationPlanFilter { Id = "shared" });
            Assert.NotNull(remaining);
            Assert.Equal(Tenant.AgnosticTenantId, remaining.TenantId);
            Assert.NotEqual("stolen", Payload(remaining));
            if (results[0] is null)
                AssertUnchangedPlan(remaining, Tenant.AgnosticTenantId, AlterationPlanStatus.Failed, "agnostic-update");
            else
                AssertUnchangedPlan(remaining, Tenant.AgnosticTenantId, AlterationPlanStatus.Running, "original");
        }
    }

    [Fact]
    public async Task SaveManyAsync_ConcurrentNamedTenantsOnEmptyJobId_OneOwnerKeepsPayload()
    {
        await using var pair = await AlterationStoreScenario.CreateSqlitePairAsync("tenant-a", "tenant-b");

        var results = await Task.WhenAll(
            Capture(() => pair.First.Jobs.SaveManyAsync([Job("race", "tenant-a", AlterationJobStatus.Running, "from-a")])),
            Capture(() => pair.Second.Jobs.SaveManyAsync([Job("race", "tenant-b", AlterationJobStatus.Completed, "from-b")])));

        Assert.Equal(1, results.Count(ex => ex is null));
        Assert.Equal(1, results.Count(ex => ex is InvalidOperationException));

        var winnerIsA = results[0] is null;
        using (pair.First.UseTenant(winnerIsA ? "tenant-a" : "tenant-b"))
        {
            var remaining = await pair.First.Jobs.FindAsync(new AlterationJobFilter { Id = "race" });
            AssertUnchangedJob(
                remaining,
                winnerIsA ? "tenant-a" : "tenant-b",
                winnerIsA ? AlterationJobStatus.Running : AlterationJobStatus.Completed,
                winnerIsA ? "from-a" : "from-b");
        }
    }

    [Fact]
    public async Task SaveManyAsync_ConcurrentNamedVersusAgnosticOnExistingStarJob_PreservesStarPayload()
    {
        await using var pair = await AlterationStoreScenario.CreateSqlitePairAsync(Tenant.AgnosticTenantId, "tenant-b");
        await pair.First.Jobs.SaveAsync(Job("shared", Tenant.AgnosticTenantId, AlterationJobStatus.Running, "original"));

        var results = await Task.WhenAll(
            Capture(() => pair.First.Jobs.SaveManyAsync([Job("shared", Tenant.AgnosticTenantId, AlterationJobStatus.Failed, "agnostic-update")])),
            Capture(() => pair.Second.Jobs.SaveManyAsync([Job("shared", "tenant-b", AlterationJobStatus.Completed, "stolen")])));

        Assert.NotNull(results[1]);
        Assert.IsType<InvalidOperationException>(results[1]);

        using (pair.First.UseTenant(Tenant.AgnosticTenantId))
        {
            var remaining = await pair.First.Jobs.FindAsync(new AlterationJobFilter { Id = "shared" });
            Assert.NotNull(remaining);
            Assert.Equal(Tenant.AgnosticTenantId, remaining.TenantId);
            Assert.NotEqual("stolen", remaining.WorkflowInstanceId);
            if (results[0] is null)
                AssertUnchangedJob(remaining, Tenant.AgnosticTenantId, AlterationJobStatus.Failed, "agnostic-update");
            else
                AssertUnchangedJob(remaining, Tenant.AgnosticTenantId, AlterationJobStatus.Running, "original");
        }
    }

    [Fact]
    public async Task SaveAsync_ConcurrentNamedTenantsOnExistingNamedPlan_PreservesOriginalOwnerAndPayload()
    {
        await using var pair = await AlterationStoreScenario.CreateSqlitePairAsync("tenant-b", "tenant-c");
        await using var owner = await AlterationStoreScenario.CreateSqliteAsync("tenant-a", pair.DatabasePath, ownsDatabaseFile: false);
        await owner.Plans.SaveAsync(Plan("shared", "tenant-a", AlterationPlanStatus.Running, "original"));

        var results = await Task.WhenAll(
            Capture(() => pair.First.Plans.SaveAsync(Plan("shared", "tenant-b", AlterationPlanStatus.Completed, "from-b"))),
            Capture(() => pair.Second.Plans.SaveAsync(Plan("shared", "tenant-c", AlterationPlanStatus.Failed, "from-c"))));

        Assert.All(results, ex => Assert.IsType<InvalidOperationException>(ex));

        var remaining = await owner.Plans.FindAsync(new AlterationPlanFilter { Id = "shared" });
        AssertUnchangedPlan(remaining, "tenant-a", AlterationPlanStatus.Running, "original");
    }

    private static async Task<Exception?> Capture(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }

    private static void AssertUnchangedPlan(AlterationPlan? plan, string tenantId, AlterationPlanStatus status, string payload)
    {
        Assert.NotNull(plan);
        Assert.Equal(tenantId, plan.TenantId);
        Assert.Equal(status, plan.Status);
        Assert.Equal(payload, Payload(plan));
    }

    private static void AssertUnchangedJob(AlterationJob? job, string tenantId, AlterationJobStatus status, string payload)
    {
        Assert.NotNull(job);
        Assert.Equal(tenantId, job.TenantId);
        Assert.Equal(status, job.Status);
        Assert.Equal(payload, job.WorkflowInstanceId);
        Assert.Equal(payload, job.Log?.FirstOrDefault()?.Message);
    }

    private static string Payload(AlterationPlan plan) =>
        Assert.IsType<TestAlteration>(Assert.Single(plan.Alterations)).Value;

    private static AlterationPlan Plan(string id, string? tenantId, AlterationPlanStatus status, string payload) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Status = status,
            CreatedAt = CreatedAt,
            Alterations = [new TestAlteration { Kind = "probe", Value = payload }],
            WorkflowInstanceFilter = new AlterationWorkflowInstanceFilter { SearchTerm = payload }
        };

    private static AlterationJob Job(string id, string? tenantId, AlterationJobStatus status, string payload) =>
        new()
        {
            Id = id,
            PlanId = "plan-1",
            WorkflowInstanceId = payload,
            Status = status,
            TenantId = tenantId,
            CreatedAt = CreatedAt,
            Log = [new AlterationLogEntry(payload, LogLevel.Information, CreatedAt, "probe")]
        };

    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);
}

/// <summary>
/// Releases the first two relevant alteration UPDATE commands after they complete, so
/// competing SaveAsync calls reach their INSERT/retry paths together. The gate is armed
/// explicitly after any setup writes so only the concurrent operation is coordinated.
/// </summary>
public sealed class GateFirstAlterationUpdates(string tableName) : DbCommandInterceptor
{
    private readonly TaskCompletionSource<bool> _bothReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _armed;
    private int _matchedCommandCount;

    public int MatchedCommandCount => Volatile.Read(ref _matchedCommandCount);

    public void Arm() => Volatile.Write(ref _armed, 1);

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (!command.CommandText.Contains($"UPDATE \"{tableName}\"", StringComparison.OrdinalIgnoreCase))
            return result;

        if (Volatile.Read(ref _armed) == 0)
            return result;

        var commandNumber = Interlocked.Increment(ref _matchedCommandCount);
        if (commandNumber <= 2)
        {
            if (commandNumber == 2)
                _bothReached.TrySetResult(true);

            await _bothReached.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        }

        return result;
    }
}
