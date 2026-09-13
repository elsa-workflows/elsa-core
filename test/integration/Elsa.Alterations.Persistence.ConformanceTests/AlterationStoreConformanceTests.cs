using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Elsa.Common.Multitenancy;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Persistence.ConformanceTests;

/// <summary>
/// Shared Memory / EF Core store-contract assertions for Alterations plan and job ports.
/// </summary>
public abstract class AlterationStoreConformanceTests
{
    protected abstract Task<AlterationStoreScenario> CreateScenarioAsync();

    [Fact]
    public async Task PlanFindAndCountHonorTenantVisibility()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedPlansAsync(scenario);

        Assert.Equal(2, await scenario.Plans.CountAsync(new AlterationPlanFilter()));
        Assert.Equal(1, await scenario.Plans.CountAsync(new AlterationPlanFilter { Id = "plan-a" }));
        Assert.Equal(0, await scenario.Plans.CountAsync(new AlterationPlanFilter { Id = "plan-b" }));
        Assert.Equal(1, await scenario.Plans.CountAsync(new AlterationPlanFilter { Id = "plan-star" }));

        Assert.Equal("plan-a", (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" }))!.Id);
        Assert.Equal("plan-star", (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-star" }))!.Id);
        Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" }));

        using (scenario.UseTenant("tenant-b"))
        {
            Assert.Equal(2, await scenario.Plans.CountAsync(new AlterationPlanFilter()));
            Assert.Equal("plan-b", (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" }))!.Id);
            Assert.Equal("plan-star", (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-star" }))!.Id);
            Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" }));
        }

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await scenario.Plans.SaveAsync(Plan("plan-default", tenantId: null));
            Assert.Equal(2, await scenario.Plans.CountAsync(new AlterationPlanFilter()));
            Assert.Equal("plan-default", (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-default" }))!.Id);
            Assert.Equal("plan-star", (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-star" }))!.Id);
            Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" }));
            Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" }));
        }

        Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-default" }));
        Assert.Equal(2, await scenario.Plans.CountAsync(new AlterationPlanFilter()));
    }

    [Fact]
    public async Task JobFindCountAndFindManyHonorTenantVisibility()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedJobsAsync(scenario);

        var found = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Id == "job-a");
        Assert.Contains(found, x => x.Id == "job-star");
        Assert.DoesNotContain(found, x => x.Id == "job-b");

        var ids = (await scenario.Jobs.FindManyIdsAsync(new AlterationJobFilter { PlanId = "plan-1" })).ToList();
        Assert.Equal(2, ids.Count);
        Assert.Contains("job-a", ids);
        Assert.Contains("job-star", ids);
        Assert.DoesNotContain("job-b", ids);

        Assert.Equal(2, await scenario.Jobs.CountAsync(new AlterationJobFilter { PlanId = "plan-1" }));
        Assert.Equal("job-a", (await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-a" }))!.Id);
        Assert.Equal("job-star", (await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-star" }))!.Id);
        Assert.Null(await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-b" }));

        using (scenario.UseTenant("tenant-b"))
        {
            var tenantB = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
            Assert.Equal(2, tenantB.Count);
            Assert.Contains(tenantB, x => x.Id == "job-b");
            Assert.Contains(tenantB, x => x.Id == "job-star");
            Assert.DoesNotContain(tenantB, x => x.Id == "job-a");
            Assert.Null(await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-a" }));
        }

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await scenario.Jobs.SaveAsync(Job("job-default", tenantId: null));
            var defaults = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
            Assert.Equal(2, defaults.Count);
            Assert.Contains(defaults, x => x.Id == "job-default");
            Assert.Contains(defaults, x => x.Id == "job-star");
            Assert.DoesNotContain(defaults, x => x.Id == "job-a");
            Assert.DoesNotContain(defaults, x => x.Id == "job-b");
        }

        Assert.Null(await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-default" }));
    }

    [Fact]
    public async Task SaveStampsAmbientTenantOnCreate()
    {
        await using var scenario = await CreateScenarioAsync();

        var plan = Plan("plan-new", tenantId: null);
        await scenario.Plans.SaveAsync(plan);
        Assert.Equal("tenant-a", plan.TenantId);
        Assert.Equal("tenant-a", (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-new" }))!.TenantId);

        var job = Job("job-new", tenantId: null);
        await scenario.Jobs.SaveAsync(job);
        Assert.Equal("tenant-a", job.TenantId);
        Assert.Equal("tenant-a", (await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-new" }))!.TenantId);

        var batch = new[] { Job("job-batch-1", tenantId: null), Job("job-batch-2", tenantId: null) };
        await scenario.Jobs.SaveManyAsync(batch);
        Assert.All(batch, x => Assert.Equal("tenant-a", x.TenantId));
        var savedBatch = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
        Assert.Contains(savedBatch, x => x.Id == "job-batch-1" && x.TenantId == "tenant-a");
        Assert.Contains(savedBatch, x => x.Id == "job-batch-2" && x.TenantId == "tenant-a");

        var agnosticPlan = Plan("plan-star", Tenant.AgnosticTenantId);
        var explicitPlan = Plan("plan-explicit", "tenant-a");
        await scenario.Plans.SaveAsync(agnosticPlan);
        await scenario.Plans.SaveAsync(explicitPlan);
        Assert.Equal(Tenant.AgnosticTenantId, agnosticPlan.TenantId);
        Assert.Equal("tenant-a", explicitPlan.TenantId);

        var agnosticJob = Job("job-star", Tenant.AgnosticTenantId);
        var explicitJob = Job("job-explicit", "tenant-a");
        await scenario.Jobs.SaveAsync(agnosticJob);
        await scenario.Jobs.SaveAsync(explicitJob);
        Assert.Equal(Tenant.AgnosticTenantId, agnosticJob.TenantId);
        Assert.Equal("tenant-a", explicitJob.TenantId);

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            var defaultPlan = Plan("plan-null", tenantId: null);
            await scenario.Plans.SaveAsync(defaultPlan);
            Assert.Equal(Tenant.DefaultTenantId, defaultPlan.TenantId);
            Assert.Equal(Tenant.DefaultTenantId, (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-null" }))!.TenantId);

            var namedPlan = Plan("plan-named", "tenant-a");
            await scenario.Plans.SaveAsync(namedPlan);
            Assert.Equal("tenant-a", namedPlan.TenantId);
            Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-named" }));

            var defaultJob = Job("job-null", tenantId: null);
            await scenario.Jobs.SaveAsync(defaultJob);
            Assert.Equal(Tenant.DefaultTenantId, defaultJob.TenantId);
            Assert.Equal(Tenant.DefaultTenantId, (await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-null" }))!.TenantId);

            var defaultBatch = new[] { Job("job-default-batch", tenantId: null) };
            await scenario.Jobs.SaveManyAsync(defaultBatch);
            Assert.Equal(Tenant.DefaultTenantId, defaultBatch[0].TenantId);
            Assert.Equal(Tenant.DefaultTenantId, (await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-default-batch" }))!.TenantId);
        }
    }

    [Fact]
    public async Task PlanFindByIdIsTenantFiltered()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedPlansAsync(scenario);

        Assert.NotNull(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" }));
        Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" }));
        Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "missing" }));

        using (scenario.UseTenant("tenant-b"))
        {
            Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" }));
            Assert.NotNull(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" }));
        }
    }

    [Fact]
    public async Task JobQueriesByPlanIdAndStatusMatchSeededFixtures()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.Jobs.SaveAsync(Job("job-pending-1", "tenant-a", planId: "plan-1", status: AlterationJobStatus.Pending));
        await scenario.Jobs.SaveAsync(Job("job-completed-1", "tenant-a", planId: "plan-1", status: AlterationJobStatus.Completed));
        await scenario.Jobs.SaveAsync(Job("job-pending-2", "tenant-a", planId: "plan-2", status: AlterationJobStatus.Pending));
        await scenario.Jobs.SaveAsync(Job("job-running-1", "tenant-a", planId: "plan-1", status: AlterationJobStatus.Running));
        await scenario.Jobs.SaveAsync(Job("job-b-pending", "tenant-b", planId: "plan-1", status: AlterationJobStatus.Pending));
        await scenario.Jobs.SaveAsync(Job("job-star-failed", Tenant.AgnosticTenantId, planId: "plan-1", status: AlterationJobStatus.Failed));

        var plan1 = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None))
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToList();
        Assert.Equal(["job-completed-1", "job-pending-1", "job-running-1", "job-star-failed"], plan1);
        Assert.Equal(4, await scenario.Jobs.CountAsync(new AlterationJobFilter { PlanId = "plan-1" }));

        var pending = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { Status = AlterationJobStatus.Pending }, CancellationToken.None))
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToList();
        Assert.Equal(["job-pending-1", "job-pending-2"], pending);

        var plan1Pending = Assert.Single(await scenario.Jobs.FindManyAsync(new AlterationJobFilter
        {
            PlanId = "plan-1",
            Status = AlterationJobStatus.Pending
        }, CancellationToken.None));
        Assert.Equal("job-pending-1", plan1Pending.Id);

        var completedOrFailed = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter
        {
            Statuses = [AlterationJobStatus.Completed, AlterationJobStatus.Failed]
        }, CancellationToken.None)).Select(x => x.Id).OrderBy(x => x).ToList();
        Assert.Equal(["job-completed-1", "job-star-failed"], completedOrFailed);

        var plan1Ids = (await scenario.Jobs.FindManyIdsAsync(new AlterationJobFilter { PlanId = "plan-1" })).OrderBy(x => x).ToList();
        Assert.Equal(["job-completed-1", "job-pending-1", "job-running-1", "job-star-failed"], plan1Ids);

        using (scenario.UseTenant("tenant-b"))
        {
            var tenantB = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None))
                .Select(x => x.Id)
                .OrderBy(x => x)
                .ToList();
            Assert.Equal(["job-b-pending", "job-star-failed"], tenantB);
            Assert.Equal(1, await scenario.Jobs.CountAsync(new AlterationJobFilter { Status = AlterationJobStatus.Pending }));
        }
    }

    [Fact]
    public async Task PlanAndJobRoundTripSerializedFields()
    {
        await using var scenario = await CreateScenarioAsync();
        var createdAt = new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);
        var startedAt = createdAt.AddMinutes(1);
        var completedAt = createdAt.AddMinutes(5);

        await scenario.Plans.SaveAsync(new AlterationPlan
        {
            Id = "plan-roundtrip",
            TenantId = "tenant-a",
            Status = AlterationPlanStatus.Running,
            CreatedAt = createdAt,
            StartedAt = startedAt,
            CompletedAt = null,
            Alterations =
            [
                new TestAlteration { Kind = "cancel", Value = "activity-1" },
                new TestAlteration { Kind = "migrate", Value = "v2" }
            ],
            WorkflowInstanceFilter = new AlterationWorkflowInstanceFilter
            {
                WorkflowInstanceIds = ["wf-1", "wf-2"],
                CorrelationIds = ["corr-1"],
                SearchTerm = "invoice",
                HasIncidents = true,
                IsSystem = false
            }
        });

        var plan = await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-roundtrip" });
        Assert.NotNull(plan);
        Assert.Equal(AlterationPlanStatus.Running, plan.Status);
        Assert.Equal(createdAt, plan.CreatedAt);
        Assert.Equal(startedAt, plan.StartedAt);
        Assert.Null(plan.CompletedAt);
        Assert.Equal(["cancel:activity-1", "migrate:v2"], plan.Alterations.Cast<TestAlteration>().Select(x => $"{x.Kind}:{x.Value}").ToList());
        Assert.Equal(["wf-1", "wf-2"], plan.WorkflowInstanceFilter.WorkflowInstanceIds);
        Assert.Equal(["corr-1"], plan.WorkflowInstanceFilter.CorrelationIds);
        Assert.Equal("invoice", plan.WorkflowInstanceFilter.SearchTerm);
        Assert.True(plan.WorkflowInstanceFilter.HasIncidents);
        Assert.False(plan.WorkflowInstanceFilter.IsSystem);

        await scenario.Jobs.SaveAsync(new AlterationJob
        {
            Id = "job-roundtrip",
            TenantId = "tenant-a",
            PlanId = "plan-roundtrip",
            WorkflowInstanceId = "wf-1",
            Status = AlterationJobStatus.Completed,
            CreatedAt = createdAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Log =
            [
                new AlterationLogEntry("started", LogLevel.Information, createdAt, "Started"),
                new AlterationLogEntry("done", LogLevel.Warning, completedAt, "Completed")
            ]
        });

        var job = await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-roundtrip" });
        Assert.NotNull(job);
        Assert.Equal("plan-roundtrip", job.PlanId);
        Assert.Equal("wf-1", job.WorkflowInstanceId);
        Assert.Equal(AlterationJobStatus.Completed, job.Status);
        Assert.Equal(createdAt, job.CreatedAt);
        Assert.Equal(startedAt, job.StartedAt);
        Assert.Equal(completedAt, job.CompletedAt);
        Assert.Equal(2, job.Log!.Count);
        Assert.Equal("started", job.Log.ElementAt(0).Message);
        Assert.Equal(LogLevel.Information, job.Log.ElementAt(0).LogLevel);
        Assert.Equal("Started", job.Log.ElementAt(0).EventName);
        Assert.Equal("done", job.Log.ElementAt(1).Message);
        Assert.Equal(LogLevel.Warning, job.Log.ElementAt(1).LogLevel);
        Assert.Equal("Completed", job.Log.ElementAt(1).EventName);
    }

    [Fact]
    public async Task CrossTenantSaveDoesNotOverwriteOrStealRows()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.Plans.SaveAsync(Plan("plan-shared", "tenant-a", AlterationPlanStatus.Pending));
        await scenario.Jobs.SaveAsync(Job("job-shared", "tenant-a", status: AlterationJobStatus.Pending));

        using (scenario.UseTenant("tenant-b"))
        {
            await scenario.AttemptSaveAsync(() =>
                scenario.Plans.SaveAsync(Plan("plan-shared", "tenant-b", AlterationPlanStatus.Completed)));
            await scenario.AttemptSaveAsync(() =>
                scenario.Jobs.SaveAsync(Job("job-shared", "tenant-b", status: AlterationJobStatus.Completed)));
            // SaveMany is not asserted here: EF BulkUpsert ignores query filters and upserts by Id.
            Assert.Null(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-shared" }));
            Assert.Null(await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-shared" }));
        }

        var plan = await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-shared" });
        Assert.NotNull(plan);
        Assert.Equal("tenant-a", plan.TenantId);
        Assert.Equal(AlterationPlanStatus.Pending, plan.Status);

        var job = await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-shared" });
        Assert.NotNull(job);
        Assert.Equal("tenant-a", job.TenantId);
        Assert.Equal(AlterationJobStatus.Pending, job.Status);

        using (scenario.UseTenant(Tenant.AgnosticTenantId))
        {
            await scenario.Plans.SaveAsync(Plan("plan-star", Tenant.AgnosticTenantId, AlterationPlanStatus.Pending));
            await scenario.Jobs.SaveAsync(Job("job-star", Tenant.AgnosticTenantId, status: AlterationJobStatus.Pending));
        }

        await scenario.AttemptSaveAsync(() =>
            scenario.Plans.SaveAsync(Plan("plan-star", "tenant-a", AlterationPlanStatus.Completed)));
        await scenario.AttemptSaveAsync(() =>
            scenario.Jobs.SaveAsync(Job("job-star", "tenant-a", status: AlterationJobStatus.Completed)));

        using (scenario.UseTenant(Tenant.AgnosticTenantId))
        {
            var starPlan = await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-star" });
            Assert.NotNull(starPlan);
            Assert.Equal(Tenant.AgnosticTenantId, starPlan.TenantId);
            Assert.Equal(AlterationPlanStatus.Pending, starPlan.Status);

            var starJob = await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-star" });
            Assert.NotNull(starJob);
            Assert.Equal(Tenant.AgnosticTenantId, starJob.TenantId);
            Assert.Equal(AlterationJobStatus.Pending, starJob.Status);

            await scenario.Plans.SaveAsync(Plan("plan-star", Tenant.AgnosticTenantId, AlterationPlanStatus.Completed));
            await scenario.Jobs.SaveAsync(Job("job-star", Tenant.AgnosticTenantId, status: AlterationJobStatus.Completed));
            Assert.Equal(AlterationPlanStatus.Completed, (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-star" }))!.Status);
            Assert.Equal(AlterationJobStatus.Completed, (await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-star" }))!.Status);
        }

        await scenario.Plans.SaveAsync(Plan("plan-shared", "tenant-a", AlterationPlanStatus.Running));
        await scenario.Jobs.SaveAsync(Job("job-shared", "tenant-a", status: AlterationJobStatus.Running));
        Assert.Equal(AlterationPlanStatus.Running, (await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-shared" }))!.Status);
        Assert.Equal(AlterationJobStatus.Running, (await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-shared" }))!.Status);
    }

    private static async Task SeedMixedPlansAsync(AlterationStoreScenario scenario)
    {
        await scenario.Plans.SaveAsync(Plan("plan-a", "tenant-a"));
        await scenario.Plans.SaveAsync(Plan("plan-b", "tenant-b"));
        await scenario.Plans.SaveAsync(Plan("plan-star", Tenant.AgnosticTenantId));
    }

    private static async Task SeedMixedJobsAsync(AlterationStoreScenario scenario)
    {
        await scenario.Jobs.SaveAsync(Job("job-a", "tenant-a"));
        await scenario.Jobs.SaveAsync(Job("job-b", "tenant-b"));
        await scenario.Jobs.SaveAsync(Job("job-star", Tenant.AgnosticTenantId));
    }

    private static AlterationPlan Plan(string id, string? tenantId, AlterationPlanStatus status = AlterationPlanStatus.Pending) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Status = status,
            CreatedAt = CreatedAt
        };

    private static AlterationJob Job(
        string id,
        string? tenantId,
        string planId = "plan-1",
        AlterationJobStatus status = AlterationJobStatus.Pending) =>
        new()
        {
            Id = id,
            PlanId = planId,
            WorkflowInstanceId = "instance-1",
            Status = status,
            TenantId = tenantId,
            CreatedAt = CreatedAt
        };

    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);
}

[CollectionDefinition(Name)]
public sealed class AlterationStoreInMemoryConformanceCollection
{
    public const string Name = "AlterationStores:InMemory";
}

[CollectionDefinition(Name)]
public sealed class AlterationStoreSqliteConformanceCollection
{
    public const string Name = "AlterationStores:EFCore.Sqlite";
}

[Collection(AlterationStoreInMemoryConformanceCollection.Name)]
public sealed class InMemoryAlterationStoreConformanceTests : AlterationStoreConformanceTests
{
    protected override Task<AlterationStoreScenario> CreateScenarioAsync() => AlterationStoreScenario.CreateInMemoryAsync();
}

[Collection(AlterationStoreSqliteConformanceCollection.Name)]
public sealed class SqliteAlterationStoreConformanceTests : AlterationStoreConformanceTests
{
    protected override Task<AlterationStoreScenario> CreateScenarioAsync() => AlterationStoreScenario.CreateSqliteAsync();
}
