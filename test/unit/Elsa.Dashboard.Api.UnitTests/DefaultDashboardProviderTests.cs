using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Services;
using Elsa.Diagnostics.ConsoleLogs.Dashboard;
using Elsa.Diagnostics.ConsoleLogs.Dashboard.Extensions;
using Elsa.Diagnostics.StructuredLogs.Dashboard;
using Elsa.Diagnostics.StructuredLogs.Dashboard.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Dashboard;
using Elsa.Workflows.Runtime.Dashboard.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
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

    public static readonly TheoryData<DashboardPermission, SectionAccess> SinglePermissions = SectionAccess.BySinglePermission.ToTheoryData();

    [Theory]
    [MemberData(nameof(SinglePermissions))]
    public async Task GetOverviewAsync_WithCallerWhoMayReadOnlyOneSection_WithholdsEveryOtherSection(DashboardPermission permission, SectionAccess readable)
    {
        var provider = CreateProvider(Declared());

        var overview = await provider.GetOverviewAsync(new() { CanRead = x => x == permission });

        readable.AssertOn(overview);
        Assert.Equal("Elsa.TestHost", overview.BackendName);
        Assert.Equal(readable.Metrics, overview.Metrics.Select(x => x.Id));
        Assert.Equal(readable.Panels, overview.Panels.Select(x => x.Id));
    }

    [Fact]
    public async Task GetOverviewAsync_WithCallerWhoMayReadTheWholeOverview_ReturnsEverything()
    {
        var provider = CreateProvider(Declared());

        var overview = await provider.GetOverviewAsync(new() { CanRead = _ => true });

        SectionAccess.All.AssertOn(overview);
        Assert.Equal(3, overview.Metrics.Count);
        Assert.Equal(3, overview.Panels.Count);
    }

    [Fact]
    public async Task GetOverviewAsync_WithSectionsThatDeclareNoPermission_NeedsTheWholeOverviewPermission()
    {
        var provider = CreateProvider(Undeclared());

        var narrow = await provider.GetOverviewAsync(new() { CanRead = permission => permission == InstancesView });
        var whole = await provider.GetOverviewAsync(new() { CanRead = permission => permission == WholeOverview });

        SectionAccess.None.AssertOn(narrow);
        Assert.Empty(narrow.Metrics);
        SectionAccess.All.AssertOn(whole);
        Assert.Single(whole.Metrics);
    }

    [Fact]
    public async Task GetOverviewAsync_WithCallerWhoMayReadNothing_WithholdsEverySectionAndTheDeploymentNames()
    {
        var provider = CreateProvider(Declared());

        var overview = await provider.GetOverviewAsync(new(DashboardRangeKeys.SevenDays) { CanRead = _ => false });

        SectionAccess.None.AssertOn(overview);
        Assert.Null(overview.BackendName);
        Assert.Null(overview.EnvironmentName);
        Assert.Equal(DashboardRangeKeys.SevenDays, overview.AppliedRange);
    }

    [Fact]
    public async Task GetOverviewAsync_WithCardOnlyTheCallerMayRead_InvokesTheContributorThatDeclaredItsPermission()
    {
        var contributor = new TestContributor("cards", 1)
        {
            Overview = new() { Metrics = [new() { Id = "card", Label = "Card", Permission = InstancesView }] },
            OverviewPermissions = new() { Runtime = RuntimeView, WorkflowInstances = InstancesView }
        };

        var overview = await CreateProvider(contributor).GetOverviewAsync(new() { CanRead = x => x == InstancesView });

        Assert.Equal(1, contributor.Invocations);
        Assert.Equal(["card"], overview.Metrics.Select(x => x.Id));
    }

    [Fact]
    public async Task GetOverviewAsync_WithCardPermissionTheContributorDidNotDeclare_SkipsItForACallerHoldingOnlyThatPermission()
    {
        var contributor = new TestContributor("under-declared", 1)
        {
            Overview = new() { Metrics = [new() { Id = "card", Label = "Card", Permission = InstancesView }] },
            OverviewPermissions = new() { Runtime = RuntimeView }
        };

        var overview = await CreateProvider(contributor).GetOverviewAsync(new() { CanRead = x => x == InstancesView });

        Assert.Equal(0, contributor.Invocations);
        Assert.Empty(overview.Metrics);
    }

    public static readonly TheoryData<DashboardPermission, string, string> FailingInstanceStoreCallers = new()
    {
        { RuntimeView, DashboardCapabilityStatus.Unauthorized.Status, DashboardRuntimeStatusKeys.AcceptingWork },
        { InstancesView, DashboardCapabilityStatus.Unavailable.Status, DashboardRuntimeStatusKeys.Unavailable }
    };

    [Theory]
    [MemberData(nameof(FailingInstanceStoreCallers))]
    public async Task GetOverviewAsync_WithFailingInstanceStore_ReportsReadableSectionsAvailableOrUnavailableNeverUnauthorized(
        DashboardPermission permission, string instancesStatus, string runtimeStatus)
    {
        var provider = CreateProvider(CreateWorkflowContributorWithFailingInstanceStore());

        var overview = await provider.GetOverviewAsync(new() { CanRead = x => x == permission });

        Assert.Equal("Elsa.TestHost", overview.BackendName);
        Assert.Equal(instancesStatus, overview.WorkflowInstances.Capability.Status);
        Assert.Equal(runtimeStatus, overview.Runtime.Status);
        Assert.Equal(permission == RuntimeView ? DashboardCapabilityStatus.Available.Status : DashboardCapabilityStatus.Unauthorized.Status, overview.Runtime.Capability.Status);
    }

    [Fact]
    public async Task GetOverviewAsync_WithFailingInstanceStoreAndCallerWhoMayReadNothing_WithholdsEverything()
    {
        var provider = CreateProvider(CreateWorkflowContributorWithFailingInstanceStore());

        var overview = await provider.GetOverviewAsync(new() { CanRead = _ => false });

        Assert.Null(overview.BackendName);
        Assert.Null(overview.EnvironmentName);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.Runtime.Capability.Status);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.WorkflowInstances.Capability.Status);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.Diagnostics.StructuredLogs.Capability.Status);
    }

    [Fact]
    public async Task GetOverviewAsync_WithContributorThatFailsForCallerWhoMayReadItsSection_ReportsItUnavailableNotUnauthorized()
    {
        var provider = CreateProvider(new ThrowingContributor("broken", 1) { OverviewPermissions = new() { Runtime = RuntimeView, WorkflowInstances = InstancesView } });

        var overview = await provider.GetOverviewAsync(new() { CanRead = x => x == InstancesView });

        Assert.Equal("Elsa.TestHost", overview.BackendName);
        Assert.Equal(DashboardCapabilityStatus.Unavailable.Status, overview.WorkflowInstances.Capability.Status);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.Runtime.Capability.Status);
    }

    [Fact]
    public async Task GetOverviewAsync_WithRuntimeOnlyCaller_DoesNotQueryTheInstanceStore()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(99L);
        store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<WorkflowInstanceSummary>());
        var runtime = Substitute.For<IWorkflowRuntimeAdminService>();
        runtime.GetStatus().Returns(new RuntimeAdminStatus(QuiescenceState.Initial("generation"), [], 0));

        var overview = await CreateProvider(new WorkflowDashboardContributor(store, runtime))
            .GetOverviewAsync(new() { CanRead = x => x == RuntimeView });

        await store.DidNotReceive().CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>());
        Assert.Equal(DashboardRuntimeStatusKeys.AcceptingWork, overview.Runtime.Status);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.WorkflowInstances.Capability.Status);
        Assert.Equal(0, overview.WorkflowInstances.Running);
    }

    [Fact]
    public async Task GetNeedsAttentionAsync_WithFailingInstanceStore_StillReturnsRuntimeFindings()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns<long>(_ => throw new InvalidOperationException("secret"));
        store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns<IEnumerable<WorkflowInstanceSummary>>(_ => throw new InvalidOperationException("secret"));
        var runtime = Substitute.For<IWorkflowRuntimeAdminService>();
        runtime.GetStatus().Returns(new RuntimeAdminStatus(QuiescenceState.Initial("generation") with { Reason = QuiescenceReason.AdministrativePause }, [], 0));

        var response = await CreateProvider(new WorkflowDashboardContributor(store, runtime))
            .GetNeedsAttentionAsync(new() { CanRead = x => x == RuntimeView }, 10);

        Assert.Equal(["runtime-paused"], response.Findings.Select(x => x.Id));
        Assert.DoesNotContain("secret", response.Findings.Single().Message);
    }

    [Fact]
    public async Task GetOverviewAsync_WithOneInstanceContributorFailingAndAnotherSucceeding_ReportsUnavailableWithoutPartialTotals()
    {
        var provider = CreateProvider(InstanceContributor("failed", 1, new() { Capability = DashboardCapabilityStatus.Unavailable }), InstanceContributor("healthy", 2, new() { Running = 5 }));

        var overview = await provider.GetOverviewAsync(new() { CanRead = x => x == InstancesView });

        Assert.Equal(DashboardCapabilityStatus.Unavailable.Status, overview.WorkflowInstances.Capability.Status);
        Assert.NotNull(overview.WorkflowInstances.Capability.Reason);
        Assert.Equal(0, overview.WorkflowInstances.Running);
    }

    [Fact]
    public async Task GetOverviewAsync_WithReadableContributorFailingAndAnUnreadableOneForTheSameSection_ReportsUnavailableNotUnauthorized()
    {
        var unreadable = new TestContributor("unreadable", 2)
        {
            Overview = new() { WorkflowInstances = new() { Running = 9 } },
            OverviewPermissions = new() { WorkflowInstances = WholeOverview, StructuredLogs = InstancesView }
        };
        var provider = CreateProvider(new ThrowingContributor("broken", 1) { OverviewPermissions = new() { WorkflowInstances = InstancesView } }, unreadable);

        var overview = await provider.GetOverviewAsync(new() { CanRead = x => x == InstancesView });

        Assert.Equal(1, unreadable.Invocations);
        Assert.Equal(DashboardCapabilityStatus.Unavailable.Status, overview.WorkflowInstances.Capability.Status);
        Assert.Equal(0, overview.WorkflowInstances.Running);
    }

    [Fact]
    public async Task GetOverviewAsync_WithOnlyUnreadableInstanceContributions_ReportsUnauthorized()
    {
        var provider = CreateProvider(InstanceContributor("failed", 1, new() { Capability = DashboardCapabilityStatus.Unavailable }), InstanceContributor("healthy", 2, new() { Running = 5 }));

        var overview = await provider.GetOverviewAsync(new() { CanRead = x => x == RuntimeView });

        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.WorkflowInstances.Capability.Status);
        Assert.Equal(0, overview.WorkflowInstances.Running);
    }

    [Fact]
    public async Task GetOverviewAsync_WithEveryReadableInstanceContributorSucceeding_MergesTotalsAsAvailable()
    {
        var provider = CreateProvider(InstanceContributor("a", 1, new() { Running = 5 }), InstanceContributor("b", 2, new() { Running = 3 }));

        var overview = await provider.GetOverviewAsync(new() { CanRead = x => x == InstancesView });

        Assert.Equal(DashboardCapabilityStatus.Available.Status, overview.WorkflowInstances.Capability.Status);
        Assert.Equal(8, overview.WorkflowInstances.Running);
    }

    private static TestContributor InstanceContributor(string id, int order, DashboardWorkflowInstanceMetrics metrics) => new(id, order)
    {
        Overview = new() { WorkflowInstances = metrics },
        OverviewPermissions = new() { WorkflowInstances = InstancesView }
    };

    private static WorkflowDashboardContributor CreateWorkflowContributorWithFailingInstanceStore()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns<long>(_ => throw new InvalidOperationException("Store down"));
        store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns<IEnumerable<WorkflowInstanceSummary>>(_ => throw new InvalidOperationException("Store down"));
        var runtime = Substitute.For<IWorkflowRuntimeAdminService>();
        runtime.GetStatus().Returns(new RuntimeAdminStatus(QuiescenceState.Initial("generation"), [], 0));
        return new(store, runtime);
    }

    [Fact]
    public async Task GetOverviewAsync_WithCallerWhoMayReadNothing_DoesNotInvokeContributorsThatDeclaredTheirPermissions()
    {
        var declared = Declared();
        var provider = CreateProvider(declared);

        await provider.GetOverviewAsync(new() { CanRead = _ => false });

        Assert.Equal(0, declared.Invocations);
    }

    [Fact]
    public async Task OtherCalls_WithCallerWhoMayReadNothing_StillInvokeContributorsThatDeclaredOverviewPermissionsAndFilterAfterwards()
    {
        var declared = Declared();
        var provider = CreateProvider(declared);
        DashboardQuery query = new() { CanRead = _ => false };

        var findings = await provider.GetNeedsAttentionAsync(query, 10);
        var trends = await provider.GetWorkflowTrendsAsync(new() { CanRead = _ => false });
        var activity = await provider.GetRecentActivityAsync(query, 10);
        var hotspots = await provider.GetWorkflowHotspotsAsync(new() { CanRead = _ => false });

        Assert.Equal(4, declared.Invocations);
        Assert.Empty(findings.Findings);
        Assert.Empty(trends.Buckets);
        Assert.Empty(activity.Items);
        Assert.Empty(hotspots.Items);
    }

    [Fact]
    public async Task GetOverviewAsync_SkipsOnlyTheContributorsWhoseEveryDeclaredPermissionIsUnreadable()
    {
        var logsOnly = new TestContributor("logs", 2)
        {
            Overview = new() { Diagnostics = new() { StructuredLogs = new() { Capability = DashboardCapabilityStatus.Available, SourceCount = 2 } } },
            OverviewPermissions = new() { StructuredLogs = StructuredLogsView }
        };
        var declared = Declared();
        var provider = CreateProvider(declared, logsOnly);

        var overview = await provider.GetOverviewAsync(new() { CanRead = permission => permission == InstancesView });

        Assert.Equal(1, declared.Invocations);
        Assert.Equal(0, logsOnly.Invocations);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.Diagnostics.StructuredLogs.Capability.Status);
    }

    [Fact]
    public async Task GetOverviewAsync_WithContributorThatDeclaredNothing_StillInvokesItAndFiltersAfterwards()
    {
        var undeclared = Undeclared();
        var provider = CreateProvider(undeclared);

        var overview = await provider.GetOverviewAsync(new() { CanRead = _ => false });

        Assert.Equal(1, undeclared.Invocations);
        SectionAccess.None.AssertOn(overview);
    }

    [Fact]
    public async Task GetOverviewAsync_WithCallerWhoMayReadNothing_DoesNotDiscloseWhichModulesAreInstalled()
    {
        var provider = CreateProvider(new WorkflowDashboardContributor(Substitute.For<IWorkflowInstanceStore>(), Substitute.For<IWorkflowRuntimeAdminService>()));

        var overview = await provider.GetOverviewAsync(new() { CanRead = _ => false });

        SectionAccess.None.AssertOn(overview);
        Assert.Null(overview.BackendName);
        Assert.Null(overview.EnvironmentName);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.Diagnostics.StructuredLogs.Capability.Status);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.Diagnostics.ConsoleLogs.Capability.Status);
    }

    [Fact]
    public async Task GetOverviewAsync_WithCallerWhoMayReadTheWholeOverview_LeavesASectionNobodySuppliedNotInstalled()
    {
        var provider = CreateProvider(new TestContributor("workflows", 1)
        {
            Overview = new() { Runtime = new() { Status = DashboardRuntimeStatusKeys.AcceptingWork }, WorkflowInstances = new() { Running = 3 } },
            OverviewPermissions = new() { Runtime = RuntimeView, WorkflowInstances = InstancesView }
        });

        var overview = await provider.GetOverviewAsync(new() { CanRead = _ => true });

        Assert.Equal("Elsa.TestHost", overview.BackendName);
        Assert.Equal(DashboardCapabilityStatus.NotInstalled.Status, overview.Diagnostics.StructuredLogs.Capability.Status);
    }

    [Fact]
    public async Task GetOverviewAsync_WithASectionItsContributorDidNotDeclare_NeedsTheWholeOverviewPermission()
    {
        var provider = CreateProvider(new TestContributor("runtime-only", 1)
        {
            Overview = new() { Runtime = new() { Status = DashboardRuntimeStatusKeys.AcceptingWork }, WorkflowInstances = new() { Running = 3 } },
            OverviewPermissions = new() { Runtime = RuntimeView }
        });

        var overview = await provider.GetOverviewAsync(new() { CanRead = permission => permission == RuntimeView });

        Assert.Equal(DashboardRuntimeStatusKeys.AcceptingWork, overview.Runtime.Status);
        Assert.Equal(DashboardCapabilityStatus.Unauthorized.Status, overview.WorkflowInstances.Capability.Status);
        Assert.Equal(0, overview.WorkflowInstances.Running);
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
        var provider = CreateProvider(Undeclared());

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
    public async Task GetWorkflowTrendsAsync_WithholdsContributionsTheCallerMayNotRead()
    {
        var provider = CreateProvider(Declared(), Undeclared());

        var narrow = await provider.GetWorkflowTrendsAsync(new() { CanRead = permission => permission == InstancesView });
        var whole = await provider.GetWorkflowTrendsAsync(new() { CanRead = _ => true });

        Assert.Equal([1L], narrow.Buckets.Select(x => x.CreatedOrStarted));
        Assert.Equal([11L], whole.Buckets.Select(x => x.CreatedOrStarted));
    }

    [Fact]
    public async Task GetRecentActivityAsync_WithholdsContributionsTheCallerMayNotRead()
    {
        var provider = CreateProvider(Declared(), Undeclared());

        var narrow = await provider.GetRecentActivityAsync(new() { CanRead = permission => permission == InstancesView }, 10);
        var whole = await provider.GetRecentActivityAsync(new() { CanRead = _ => true }, 10);

        Assert.Equal(["instance"], narrow.Items.Select(x => x.InstanceId));
        Assert.Equal(["instance", "undeclared"], whole.Items.Select(x => x.InstanceId).Order());
    }

    [Fact]
    public async Task GetWorkflowHotspotsAsync_WithholdsContributionsTheCallerMayNotRead()
    {
        var provider = CreateProvider(Declared(), Undeclared());

        var narrow = await provider.GetWorkflowHotspotsAsync(new() { CanRead = permission => permission == InstancesView });
        var whole = await provider.GetWorkflowHotspotsAsync(new() { CanRead = _ => true });

        Assert.Equal(["definition"], narrow.Items.Select(x => x.DefinitionId));
        Assert.Equal(["definition", "undeclared"], whole.Items.Select(x => x.DefinitionId).Order());
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

        public DashboardOverviewPermissions? OverviewPermissions { get; init; }

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
