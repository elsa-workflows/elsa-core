using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Services;
using Elsa.Diagnostics.ConsoleLogs.Dashboard;
using Elsa.Diagnostics.ConsoleLogs.Dashboard.Extensions;
using Elsa.Diagnostics.StructuredLogs.Dashboard;
using Elsa.Diagnostics.StructuredLogs.Dashboard.Extensions;
using Elsa.Workflows.Runtime.Dashboard;
using Elsa.Workflows.Runtime.Dashboard.Extensions;
using Microsoft.Extensions.DependencyInjection;
using static Elsa.Dashboard.Api.UnitTests.TestContributor;

namespace Elsa.Dashboard.Api.UnitTests;

public class DefaultDashboardProviderTests
{
    private readonly DateTimeOffset _now = new(2026, 06, 01, 12, 00, 00, TimeSpan.Zero);

    [Fact]
    public async Task GetOverviewAsync_WithNoContributors_ReturnsStableEmptySnapshot()
    {
        var provider = CreateProvider();

        var overview = await provider.GetOverviewAsync(new(DashboardRangeKeys.TwentyFourHours));

        Assert.Equal("Elsa.TestHost", overview.BackendName);
        Assert.Equal("Integration", overview.EnvironmentName);
        Assert.Equal(DashboardRuntimeStatusKeys.Unavailable, overview.Runtime.Status);
        Assert.Equal(0, overview.WorkflowInstances.Running);
        Assert.Equal(DashboardCapabilityStatus.NotInstalled.Status, overview.Diagnostics.StructuredLogs.Capability.Status);
        Assert.Equal(DashboardCapabilityStatus.NotInstalled.Status, overview.Diagnostics.ConsoleLogs.Capability.Status);
        Assert.Empty(overview.Metrics);
        Assert.Empty(overview.Panels);
    }

    [Fact]
    public async Task GetOverviewAsync_ComposesWorkflowAndDiagnosticsContributors()
    {
        var provider = CreateProvider(
            new TestContributor("diagnostics", 200)
            {
                Overview = new()
                {
                    Diagnostics = new()
                    {
                        StructuredLogs = new()
                        {
                            Capability = DashboardCapabilityStatus.Available,
                            SourceCount = 2
                        }
                    }
                }
            },
            new TestContributor("workflows", 100)
            {
                Overview = new()
                {
                    Runtime = new()
                    {
                        Status = DashboardRuntimeStatusKeys.AcceptingWork,
                        IsAcceptingWork = true
                    },
                    WorkflowInstances = new()
                    {
                        Running = 3,
                        Completed = 5,
                        Faulted = 1
                    }
                }
            });

        var overview = await provider.GetOverviewAsync(new(DashboardRangeKeys.TwentyFourHours));

        Assert.Equal(DashboardRuntimeStatusKeys.AcceptingWork, overview.Runtime.Status);
        Assert.Equal(3, overview.WorkflowInstances.Running);
        Assert.Equal(5, overview.WorkflowInstances.Completed);
        Assert.Equal(1, overview.WorkflowInstances.Faulted);
        Assert.Equal(DashboardCapabilityStatus.Available.Status, overview.Diagnostics.StructuredLogs.Capability.Status);
        Assert.Equal(2, overview.Diagnostics.StructuredLogs.SourceCount);
    }

    [Fact]
    public async Task GetOverviewAsync_WithCallerWhoMayReadOnlyInstances_WithholdsEveryOtherSection()
    {
        var provider = CreateProvider(Declared());

        var overview = await provider.GetOverviewAsync(new() { CanRead = permission => permission == InstancesView });

        Assert.Equal(3, overview.WorkflowInstances.Running);
        Assert.Equal(DashboardCapabilityStatus.Available.Status, overview.WorkflowInstances.Capability.Status);
        AssertWithheld(overview, runtime: true, structuredLogs: true, consoleLogs: true);
        Assert.Equal([InstancesMetric], overview.Metrics.Select(x => x.Id));
        Assert.Equal([InstancesPanel], overview.Panels.Select(x => x.Id));
    }

    [Fact]
    public async Task GetOverviewAsync_WithCallerWhoMayReadOnlyStructuredLogs_WithholdsEveryOtherSection()
    {
        var provider = CreateProvider(Declared());

        var overview = await provider.GetOverviewAsync(new() { CanRead = permission => permission == StructuredLogsView });

        Assert.Equal(DashboardCapabilityStatus.Available.Status, overview.Diagnostics.StructuredLogs.Capability.Status);
        Assert.Equal(2, overview.Diagnostics.StructuredLogs.SourceCount);
        AssertWithheld(overview, runtime: true, instances: true, consoleLogs: true);
        Assert.Empty(overview.Metrics);
        Assert.Equal([LogsPanel], overview.Panels.Select(x => x.Id));
    }

    [Fact]
    public async Task GetOverviewAsync_WithCallerWhoMayReadTheWholeOverview_ReturnsEverything()
    {
        var provider = CreateProvider(Declared());

        var overview = await provider.GetOverviewAsync(new() { CanRead = _ => true });

        AssertWithheld(overview);
        Assert.Equal(DashboardRuntimeStatusKeys.AcceptingWork, overview.Runtime.Status);
        Assert.Equal(3, overview.WorkflowInstances.Running);
        Assert.Equal(2, overview.Diagnostics.StructuredLogs.SourceCount);
        Assert.Equal(4, overview.Diagnostics.ConsoleLogs.SourceCount);
        Assert.Equal(3, overview.Metrics.Count);
        Assert.Equal(3, overview.Panels.Count);
    }

    [Fact]
    public async Task GetOverviewAsync_WithSectionsThatDeclareNoPermission_NeedsTheWholeOverviewPermission()
    {
        var undeclared = new TestContributor("undeclared", 1)
        {
            Overview = new()
            {
                WorkflowInstances = new() { Running = 3 },
                Metrics = [new() { Id = "undeclared", Label = "Undeclared" }]
            }
        };
        var provider = CreateProvider(undeclared);

        var narrow = await provider.GetOverviewAsync(new() { CanRead = permission => permission == InstancesView });
        var whole = await provider.GetOverviewAsync(new() { CanRead = permission => permission == WholeOverview });

        AssertWithheld(narrow, instances: true);
        Assert.Empty(narrow.Metrics);
        Assert.Equal(3, whole.WorkflowInstances.Running);
        Assert.Single(whole.Metrics);
    }

    [Fact]
    public async Task GetOverviewAsync_WithASectionNobodySupplied_LeavesItAtItsDefaultForACallerWhoMayReadNothing()
    {
        var provider = CreateProvider();

        var overview = await provider.GetOverviewAsync(new() { CanRead = _ => false });

        Assert.Equal(DashboardCapabilityStatus.NotInstalled.Status, overview.Diagnostics.StructuredLogs.Capability.Status);
        Assert.Equal(DashboardCapabilityStatus.Available.Status, overview.WorkflowInstances.Capability.Status);
    }

    [Fact]
    public async Task GetNeedsAttentionAsync_WithholdsFindingsTheCallerMayNotRead()
    {
        var provider = CreateProvider(Declared());

        var response = await provider.GetNeedsAttentionAsync(new() { CanRead = permission => permission == InstancesView }, 10);

        Assert.Equal([InstancesFinding], response.Findings.Select(x => x.Id));
    }

    [Fact]
    public async Task GetNeedsAttentionAsync_WithFindingsThatDeclareNoPermission_NeedsTheWholeOverviewPermission()
    {
        var provider = CreateProvider(new TestContributor("undeclared", 1) { Findings = [new() { Id = "undeclared", Message = "Undeclared" }] });

        var narrow = await provider.GetNeedsAttentionAsync(new() { CanRead = permission => permission == InstancesView }, 10);
        var whole = await provider.GetNeedsAttentionAsync(new() { CanRead = permission => permission == WholeOverview }, 10);

        Assert.Empty(narrow.Findings);
        Assert.Single(whole.Findings);
    }

    [Fact]
    public async Task GetNeedsAttentionAsync_ReturnsContributorFindingsInDeterministicOrder()
    {
        var provider = CreateProvider(
            new TestContributor("b", 20)
            {
                Findings =
                [
                    new() { Id = "second-b", Message = "Second B", Priority = 20 },
                    new() { Id = "second-a", Message = "Second A", Priority = 20 }
                ]
            },
            new TestContributor("a", 10)
            {
                Findings =
                [
                    new() { Id = "first", Message = "First", Priority = 10 }
                ]
            });

        var response = await provider.GetNeedsAttentionAsync(new(DashboardRangeKeys.TwentyFourHours), 10);

        Assert.Collection(response.Findings,
            finding => Assert.Equal("first", finding.Id),
            finding => Assert.Equal("second-a", finding.Id),
            finding => Assert.Equal("second-b", finding.Id));
    }

    [Fact]
    public async Task ContributorFailure_DoesNotBreakDashboard()
    {
        var provider = CreateProvider(
            new ThrowingContributor("broken", 1),
            new TestContributor("healthy", 2)
            {
                Overview = new()
                {
                    WorkflowInstances = new()
                    {
                        Running = 7
                    }
                },
                Findings =
                [
                    new() { Id = "healthy", Message = "Healthy", Priority = 10 }
                ]
            });

        var overview = await provider.GetOverviewAsync(new(DashboardRangeKeys.TwentyFourHours));
        var needsAttention = await provider.GetNeedsAttentionAsync(new(DashboardRangeKeys.TwentyFourHours), 10);

        Assert.Equal(7, overview.WorkflowInstances.Running);
        Assert.Single(needsAttention.Findings);
    }

    [Fact]
    public async Task RequestCancellation_IsNotSwallowed()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var provider = CreateProvider(new CanceledContributor());

        await Assert.ThrowsAsync<OperationCanceledException>(() => provider.GetOverviewAsync(new(), cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetWorkflowTrendsAsync_AggregatesContributorBuckets()
    {
        var from = _now.AddHours(-1);
        var provider = CreateProvider(
            new TestContributor("a", 1)
            {
                Trend = new()
                {
                    Buckets = [new() { From = from, To = _now, CreatedOrStarted = 1, Faulted = 2 }]
                }
            },
            new TestContributor("b", 2)
            {
                Trend = new()
                {
                    Buckets = [new() { From = from, To = _now, CreatedOrStarted = 3, Finished = 4 }]
                }
            });

        var response = await provider.GetWorkflowTrendsAsync(new() { Range = DashboardRangeKeys.OneHour, Granularity = DashboardTrendGranularity.Hour });
        var bucket = Assert.Single(response.Buckets);

        Assert.Equal(4, bucket.CreatedOrStarted);
        Assert.Equal(4, bucket.Finished);
        Assert.Equal(2, bucket.Faulted);
    }

    [Fact]
    public async Task GetRecentActivityAsync_MergesAndLimitsContributorItems()
    {
        var provider = CreateProvider(
            new TestContributor("a", 1)
            {
                RecentActivity = new()
                {
                    Items = [Recent("old", _now.AddMinutes(-10)), Recent("new", _now)]
                }
            },
            new TestContributor("b", 2)
            {
                RecentActivity = new()
                {
                    Items = [Recent("middle", _now.AddMinutes(-5))]
                }
            });

        var response = await provider.GetRecentActivityAsync(new(DashboardRangeKeys.OneHour), 2);

        Assert.Collection(response.Items,
            item => Assert.Equal("new", item.InstanceId),
            item => Assert.Equal("middle", item.InstanceId));
    }

    [Fact]
    public void DashboardApiProject_DoesNotReferenceWorkflowOrDiagnosticsModules()
    {
        var projectFile = FindRepositoryRoot().Combine("src/modules/Elsa.Dashboard.Api/Elsa.Dashboard.Api.csproj");
        var project = File.ReadAllText(projectFile);

        Assert.DoesNotContain("Elsa.Workflows", project);
        Assert.DoesNotContain("Elsa.Diagnostics", project);
        Assert.Contains("Elsa.Dashboard.Abstractions", project);
    }

    [Fact]
    public void OwnerProjects_DoNotReferenceDashboardAbstractions()
    {
        var root = FindRepositoryRoot();
        var ownerProjects = new[]
        {
            "src/modules/Elsa.Workflows.Runtime/Elsa.Workflows.Runtime.csproj",
            "src/modules/Elsa.Diagnostics.ConsoleLogs/Elsa.Diagnostics.ConsoleLogs.csproj",
            "src/modules/Elsa.Diagnostics.StructuredLogs/Elsa.Diagnostics.StructuredLogs.csproj"
        };

        foreach (var ownerProject in ownerProjects)
        {
            var project = File.ReadAllText(root.Combine(ownerProject));
            Assert.DoesNotContain("Elsa.Dashboard.Abstractions", project);
        }
    }

    [Fact]
    public void DashboardCompanionProjects_RegisterExpectedContributors()
    {
        var services = new ServiceCollection();

        services
            .AddWorkflowRuntimeDashboard()
            .AddStructuredLogsDashboard()
            .AddConsoleLogsDashboard();

        AssertRegisteredContributor<WorkflowDashboardContributor>(services);
        AssertRegisteredContributor<StructuredLogsDashboardContributor>(services);
        AssertRegisteredContributor<ConsoleLogsDashboardContributor>(services);
        Assert.Equal(3, services.Count(x => x.ServiceType == typeof(IDashboardContributor)));
    }

    private static void AssertWithheld(DashboardOverview overview, bool runtime = false, bool instances = false, bool structuredLogs = false, bool consoleLogs = false)
    {
        AssertWithheld(runtime, overview.Runtime.Capability, overview.Runtime.Status != DashboardRuntimeStatusKeys.AcceptingWork);
        AssertWithheld(instances, overview.WorkflowInstances.Capability, overview.WorkflowInstances.Running == 0);
        AssertWithheld(structuredLogs, overview.Diagnostics.StructuredLogs.Capability, overview.Diagnostics.StructuredLogs.SourceCount == 0);
        AssertWithheld(consoleLogs, overview.Diagnostics.ConsoleLogs.Capability, overview.Diagnostics.ConsoleLogs.SourceCount == 0);
    }

    /// <summary>A withheld section is marked Unauthorized and carries no data; any other is not marked.</summary>
    private static void AssertWithheld(bool expected, DashboardCapabilityStatus capability, bool hasNoData)
    {
        Assert.Equal(expected, capability.Status == DashboardCapabilityStatus.Unauthorized.Status);
        Assert.True(!expected || hasNoData);
    }

    private DefaultDashboardProvider CreateProvider(params IDashboardContributor[] contributors) =>
        new(contributors, new(new TestClock(_now)), new TestHostEnvironment());

    private static void AssertRegisteredContributor<TContributor>(IServiceCollection services)
    {
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IDashboardContributor) &&
            descriptor.ImplementationType == typeof(TContributor) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    private static DashboardRecentActivityItem Recent(string id, DateTimeOffset updatedAt) => new()
    {
        InstanceId = id,
        DefinitionId = "workflow",
        Status = "Finished",
        SubStatus = "Finished",
        CreatedAt = updatedAt.AddMinutes(-1),
        UpdatedAt = updatedAt
    };

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elsa.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed class ThrowingContributor(string id, int order) : IDashboardContributor
    {
        public string Id { get; } = id;

        public int Order { get; } = order;

        public ValueTask<DashboardOverviewContribution?> GetOverviewAsync(DashboardContext context) => throw new InvalidOperationException("Broken");

        public ValueTask<IReadOnlyCollection<DashboardFinding>> GetFindingsAsync(DashboardContext context) => throw new InvalidOperationException("Broken");
    }

    private sealed class CanceledContributor : IDashboardContributor
    {
        public string Id => "canceled";

        public int Order => 0;

        public ValueTask<DashboardOverviewContribution?> GetOverviewAsync(DashboardContext context) => throw new OperationCanceledException(context.CancellationToken);
    }
}

internal static class DirectoryInfoExtensions
{
    public static string Combine(this DirectoryInfo directory, string path) => Path.Combine(directory.FullName, path);
}
