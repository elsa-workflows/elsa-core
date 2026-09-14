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

    [Test]
    public async Task PlanFindAndCountHonorTenantVisibility()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedPlansAsync(scenario);

        await Assert.That(await scenario.Plans.CountAsync(new AlterationPlanFilter())).IsEqualTo(2);
        await Assert.That(await scenario.Plans.CountAsync(new AlterationPlanFilter { Id = "plan-a" })).IsEqualTo(1);
        await Assert.That(await scenario.Plans.CountAsync(new AlterationPlanFilter { Id = "plan-b" })).IsEqualTo(0);
        await Assert.That(await scenario.Plans.CountAsync(new AlterationPlanFilter { Id = "plan-star" })).IsEqualTo(1);

        await Assert.That((await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" }))!.Id).IsEqualTo("plan-a");
        await Assert.That((await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-star" }))!.Id).IsEqualTo("plan-star");
        await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" })).IsNull();

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.That(await scenario.Plans.CountAsync(new AlterationPlanFilter())).IsEqualTo(2);
            await Assert.That((await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" }))!.Id).IsEqualTo("plan-b");
            await Assert.That((await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-star" }))!.Id).IsEqualTo("plan-star");
            await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" })).IsNull();
        }

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await scenario.Plans.SaveAsync(Plan("plan-default", tenantId: null));
            await Assert.That(await scenario.Plans.CountAsync(new AlterationPlanFilter())).IsEqualTo(2);
            await Assert.That((await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-default" }))!.Id).IsEqualTo("plan-default");
            await Assert.That((await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-star" }))!.Id).IsEqualTo("plan-star");
            await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" })).IsNull();
            await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" })).IsNull();
        }

        await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-default" })).IsNull();
        await Assert.That(await scenario.Plans.CountAsync(new AlterationPlanFilter())).IsEqualTo(2);
    }

    [Test]
    public async Task JobFindCountAndFindManyHonorTenantVisibility()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedJobsAsync(scenario);

        var found = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains(x => x.Id == "job-a");
        await Assert.That(found).Contains(x => x.Id == "job-star");
        await Assert.That(found).DoesNotContain(x => x.Id == "job-b");

        var ids = (await scenario.Jobs.FindManyIdsAsync(new AlterationJobFilter { PlanId = "plan-1" })).ToList();
        await Assert.That(ids.Count).IsEqualTo(2);
        await Assert.That(ids).Contains("job-a");
        await Assert.That(ids).Contains("job-star");
        await Assert.That(ids).DoesNotContain("job-b");

        await Assert.That(await scenario.Jobs.CountAsync(new AlterationJobFilter { PlanId = "plan-1" })).IsEqualTo(2);
        await Assert.That((await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-a" }))!.Id).IsEqualTo("job-a");
        await Assert.That((await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-star" }))!.Id).IsEqualTo("job-star");
        await Assert.That(await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-b" })).IsNull();

        using (scenario.UseTenant("tenant-b"))
        {
            var tenantB = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
            await Assert.That(tenantB.Count).IsEqualTo(2);
            await Assert.That(tenantB).Contains(x => x.Id == "job-b");
            await Assert.That(tenantB).Contains(x => x.Id == "job-star");
            await Assert.That(tenantB).DoesNotContain(x => x.Id == "job-a");
            await Assert.That(await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-a" })).IsNull();
        }

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await scenario.Jobs.SaveAsync(Job("job-default", tenantId: null));
            var defaults = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
            await Assert.That(defaults.Count).IsEqualTo(2);
            await Assert.That(defaults).Contains(x => x.Id == "job-default");
            await Assert.That(defaults).Contains(x => x.Id == "job-star");
            await Assert.That(defaults).DoesNotContain(x => x.Id == "job-a");
            await Assert.That(defaults).DoesNotContain(x => x.Id == "job-b");
        }

        await Assert.That(await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-default" })).IsNull();
    }

    [Test]
    public async Task SaveStampsAmbientTenantOnCreate()
    {
        await using var scenario = await CreateScenarioAsync();

        var plan = Plan("plan-new", tenantId: null);
        await scenario.Plans.SaveAsync(plan);
        await Assert.That(plan.TenantId).IsEqualTo("tenant-a");
        await Assert.That((await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-new" }))!.TenantId).IsEqualTo("tenant-a");

        var job = Job("job-new", tenantId: null);
        await scenario.Jobs.SaveAsync(job);
        await Assert.That(job.TenantId).IsEqualTo("tenant-a");
        await Assert.That((await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-new" }))!.TenantId).IsEqualTo("tenant-a");

        var batch = new[] { Job("job-batch-1", tenantId: null), Job("job-batch-2", tenantId: null) };
        await scenario.Jobs.SaveManyAsync(batch);
        await Assert.That(batch).All(x => x.TenantId == "tenant-a");
        var savedBatch = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None)).ToList();
        await Assert.That(savedBatch).Contains(x => x.Id == "job-batch-1" && x.TenantId == "tenant-a");
        await Assert.That(savedBatch).Contains(x => x.Id == "job-batch-2" && x.TenantId == "tenant-a");

        var agnosticPlan = Plan("plan-star", Tenant.AgnosticTenantId);
        var explicitPlan = Plan("plan-explicit", "tenant-a");
        await scenario.Plans.SaveAsync(agnosticPlan);
        await scenario.Plans.SaveAsync(explicitPlan);
        await Assert.That(agnosticPlan.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
        await Assert.That(explicitPlan.TenantId).IsEqualTo("tenant-a");

        var agnosticJob = Job("job-star", Tenant.AgnosticTenantId);
        var explicitJob = Job("job-explicit", "tenant-a");
        await scenario.Jobs.SaveAsync(agnosticJob);
        await scenario.Jobs.SaveAsync(explicitJob);
        await Assert.That(agnosticJob.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
        await Assert.That(explicitJob.TenantId).IsEqualTo("tenant-a");

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            var defaultPlan = Plan("plan-null", tenantId: null);
            await scenario.Plans.SaveAsync(defaultPlan);
            await Assert.That(defaultPlan.TenantId).IsEqualTo(Tenant.DefaultTenantId);
            await Assert.That((await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-null" }))!.TenantId).IsEqualTo(Tenant.DefaultTenantId);

            var namedPlan = Plan("plan-named", "tenant-a");
            await scenario.Plans.SaveAsync(namedPlan);
            await Assert.That(namedPlan.TenantId).IsEqualTo("tenant-a");
            await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-named" })).IsNull();

            var defaultJob = Job("job-null", tenantId: null);
            await scenario.Jobs.SaveAsync(defaultJob);
            await Assert.That(defaultJob.TenantId).IsEqualTo(Tenant.DefaultTenantId);
            await Assert.That((await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-null" }))!.TenantId).IsEqualTo(Tenant.DefaultTenantId);

            var defaultBatch = new[] { Job("job-default-batch", tenantId: null) };
            await scenario.Jobs.SaveManyAsync(defaultBatch);
            await Assert.That(defaultBatch[0].TenantId).IsEqualTo(Tenant.DefaultTenantId);
            await Assert.That((await scenario.Jobs.FindAsync(new AlterationJobFilter { Id = "job-default-batch" }))!.TenantId).IsEqualTo(Tenant.DefaultTenantId);
        }
    }

    [Test]
    public async Task PlanFindByIdIsTenantFiltered()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedPlansAsync(scenario);

        await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" })).IsNotNull();
        await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" })).IsNull();
        await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "missing" })).IsNull();

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-a" })).IsNull();
            await Assert.That(await scenario.Plans.FindAsync(new AlterationPlanFilter { Id = "plan-b" })).IsNotNull();
        }
    }

    [Test]
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
        await Assert.That(plan1).IsEquivalentTo(["job-completed-1", "job-pending-1", "job-running-1", "job-star-failed"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(await scenario.Jobs.CountAsync(new AlterationJobFilter { PlanId = "plan-1" })).IsEqualTo(4);

        var pending = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { Status = AlterationJobStatus.Pending }, CancellationToken.None))
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToList();
        await Assert.That(pending).IsEquivalentTo(["job-pending-1", "job-pending-2"], TUnit.Assertions.Enums.CollectionOrdering.Matching);

        var plan1Pending = (await Assert.That(await scenario.Jobs.FindManyAsync(new AlterationJobFilter
        {
            PlanId = "plan-1",
            Status = AlterationJobStatus.Pending
        }, CancellationToken.None)).HasSingleItem());
        await Assert.That(plan1Pending.Id).IsEqualTo("job-pending-1");

        var completedOrFailed = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter
        {
            Statuses = [AlterationJobStatus.Completed, AlterationJobStatus.Failed]
        }, CancellationToken.None)).Select(x => x.Id).OrderBy(x => x).ToList();
        await Assert.That(completedOrFailed).IsEquivalentTo(["job-completed-1", "job-star-failed"], TUnit.Assertions.Enums.CollectionOrdering.Matching);

        var plan1Ids = (await scenario.Jobs.FindManyIdsAsync(new AlterationJobFilter { PlanId = "plan-1" })).OrderBy(x => x).ToList();
        await Assert.That(plan1Ids).IsEquivalentTo(["job-completed-1", "job-pending-1", "job-running-1", "job-star-failed"], TUnit.Assertions.Enums.CollectionOrdering.Matching);

        using (scenario.UseTenant("tenant-b"))
        {
            var tenantB = (await scenario.Jobs.FindManyAsync(new AlterationJobFilter { PlanId = "plan-1" }, CancellationToken.None))
                .Select(x => x.Id)
                .OrderBy(x => x)
                .ToList();
            await Assert.That(tenantB).IsEquivalentTo(["job-b-pending", "job-star-failed"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
            await Assert.That(await scenario.Jobs.CountAsync(new AlterationJobFilter { Status = AlterationJobStatus.Pending })).IsEqualTo(1);
        }
    }

    [Test]
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
        await Assert.That(plan).IsNotNull();
        await Assert.That(plan.Status).IsEqualTo(AlterationPlanStatus.Running);
        await Assert.That(plan.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(plan.StartedAt).IsEqualTo(startedAt);
        await Assert.That(plan.CompletedAt).IsNull();
        await Assert.That(plan.Alterations.Cast<TestAlteration>().Select(x => $"{x.Kind}:{x.Value}").ToList()).IsEquivalentTo(["cancel:activity-1", "migrate:v2"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(plan.WorkflowInstanceFilter.WorkflowInstanceIds).IsEquivalentTo(["wf-1", "wf-2"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(plan.WorkflowInstanceFilter.CorrelationIds).IsEquivalentTo(["corr-1"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(plan.WorkflowInstanceFilter.SearchTerm).IsEqualTo("invoice");
        await Assert.That(plan.WorkflowInstanceFilter.HasIncidents).IsTrue();
        await Assert.That(plan.WorkflowInstanceFilter.IsSystem).IsFalse();

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
        await Assert.That(job).IsNotNull();
        await Assert.That(job.PlanId).IsEqualTo("plan-roundtrip");
        await Assert.That(job.WorkflowInstanceId).IsEqualTo("wf-1");
        await Assert.That(job.Status).IsEqualTo(AlterationJobStatus.Completed);
        await Assert.That(job.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(job.StartedAt).IsEqualTo(startedAt);
        await Assert.That(job.CompletedAt).IsEqualTo(completedAt);
        await Assert.That(job.Log!.Count).IsEqualTo(2);
        await Assert.That(job.Log.ElementAt(0).Message).IsEqualTo("started");
        await Assert.That(job.Log.ElementAt(0).LogLevel).IsEqualTo(LogLevel.Information);
        await Assert.That(job.Log.ElementAt(0).EventName).IsEqualTo("Started");
        await Assert.That(job.Log.ElementAt(1).Message).IsEqualTo("done");
        await Assert.That(job.Log.ElementAt(1).LogLevel).IsEqualTo(LogLevel.Warning);
        await Assert.That(job.Log.ElementAt(1).EventName).IsEqualTo("Completed");
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

[InheritsTests]
public sealed class InMemoryAlterationStoreConformanceTests : AlterationStoreConformanceTests
{
    protected override Task<AlterationStoreScenario> CreateScenarioAsync() => AlterationStoreScenario.CreateInMemoryAsync();
}

[InheritsTests]
public sealed class SqliteAlterationStoreConformanceTests : AlterationStoreConformanceTests
{
    protected override Task<AlterationStoreScenario> CreateScenarioAsync() => AlterationStoreScenario.CreateSqliteAsync();
}
