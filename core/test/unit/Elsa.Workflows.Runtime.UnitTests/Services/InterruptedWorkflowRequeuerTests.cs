using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class InterruptedWorkflowRequeuerTests
{
    private readonly IWorkflowRestarter _restarter = Substitute.For<IWorkflowRestarter>();
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly DefaultTenantAccessor _tenantAccessor = new();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly List<(string InstanceId, string? TenantId)> _restarts = [];

    public InterruptedWorkflowRequeuerTests()
    {
        _tenantService.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ci =>
        {
            var tenantId = ci.ArgAt<string>(0);
            return Task.FromResult<Tenant?>(tenantId.StartsWith("gone") ? null : new Tenant { Id = tenantId, Name = tenantId });
        });
        _restarter
            .When(x => x.RestartWorkflowAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(ci => _restarts.Add((ci.ArgAt<string>(0), _tenantAccessor.Tenant?.Id)));
    }

    [Fact(DisplayName = "Instances of an unknown tenant are skipped with one warning per tenant and not counted")]
    public async Task UnknownTenant_SkippedWithOneWarningPerTenant()
    {
        var count = await RequeueAsync(Summary("a", "gone-1"), Summary("b", "gone-1"), Summary("c", "gone-2"), Summary("d", "t1"));

        Assert.Equal(1, count);
        Assert.Equal([("d", "t1")], _restarts);
        Assert.Equal([2, 1], WarningCounts());
        await _tenantService.Received(1).FindAsync("gone-1", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A failed tenant lookup is not cached, so the next instance of that tenant looks it up again")]
    public async Task FailedTenantLookup_NotCached()
    {
        _tenantService.FindAsync("t1", Arg.Any<CancellationToken>()).Returns(
            _ => throw new InvalidOperationException("lookup failed"),
            _ => Task.FromResult<Tenant?>(new Tenant { Id = "t1", Name = "t1" }));

        var count = await RequeueAsync(Summary("a", "t1"), Summary("b", "t1"));

        Assert.Equal(1, count);
        Assert.Equal([("b", "t1")], _restarts);
        await _tenantService.Received(2).FindAsync("t1", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "When no default tenant is configured, empty, null and agnostic ids restart under a synthesized default tenant")]
    public async Task UnconfiguredDefaultTenant_SynthesizedDefaultUsed()
    {
        _tenantService.FindAsync(Tenant.DefaultTenantId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Tenant?>(null));

        var count = await RequeueAsync(Summary("a", ""), Summary("b", null), Summary("c", "*"));

        Assert.Equal(3, count);
        Assert.Equal([("a", Tenant.DefaultTenantId), ("b", Tenant.DefaultTenantId), ("c", Tenant.DefaultTenantId)], _restarts);
        Assert.Empty(WarningCounts());
        await _tenantService.Received(1).FindAsync(Tenant.DefaultTenantId, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Every instance is restarted once when requeueing removes rows from the set between pages")]
    public async Task ShrinkingSet_RestartsEveryInstanceOnce()
    {
        var remaining = Enumerable.Range(1, 7).Select(i => Summary($"w-{i}", "t1")).ToList();
        _restarter
            .When(x => x.RestartWorkflowAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(ci => remaining.RemoveAll(x => x.Id == ci.ArgAt<string>(0)));

        var count = await RequeueAsync(PageThrough(remaining, 2));

        Assert.Equal(7, count);
        Assert.Equal(Enumerable.Range(1, 7).Select(i => $"w-{i}"), _restarts.Select(x => x.InstanceId).Order());
        Assert.Empty(remaining);
    }

    [Fact(DisplayName = "An instance returned on more than one page is restarted once")]
    public async Task DuplicateIdsAcrossPages_RestartedOnce()
    {
        var count = await RequeueAsync(PageThrough([Summary("a", "t1"), Summary("b", "t1"), Summary("b", "t1"), Summary("c", "t1")], 2));

        Assert.Equal(3, count);
        Assert.Equal(["a", "b", "c"], _restarts.Select(x => x.InstanceId));
    }

    [Fact(DisplayName = "Cancellation during a restart propagates, stops the run and is not logged as an error")]
    public async Task Cancellation_PropagatesWithoutErrorLog()
    {
        using var cts = new CancellationTokenSource();
        _restarter.RestartWorkflowAsync("a", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        });

        await Assert.ThrowsAsync<OperationCanceledException>(() => RequeueAsync(PageThrough([Summary("a", "t1"), Summary("b", "t1")], 10), cts.Token));

        await _restarter.DidNotReceive().RestartWorkflowAsync("b", Arg.Any<CancellationToken>());
        Assert.DoesNotContain(_logger.ReceivedCalls(), x => x.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)x.GetArguments()[0]! == LogLevel.Error);
    }

    [Fact(DisplayName = "Cancellation requested while a restart succeeds stops the run before the next instance")]
    public async Task CancellationAfterSuccessfulRestart_StopsBeforeNextInstance()
    {
        using var cts = new CancellationTokenSource();
        _restarter
            .When(x => x.RestartWorkflowAsync("a", Arg.Any<CancellationToken>()))
            .Do(_ => cts.Cancel());

        await Assert.ThrowsAsync<OperationCanceledException>(() => RequeueAsync(PageThrough([Summary("a", "t1"), Summary("b", "t1")], 10), cts.Token));

        await _restarter.DidNotReceive().RestartWorkflowAsync("b", Arg.Any<CancellationToken>());
    }

    private Task<int> RequeueAsync(params WorkflowInstanceSummary[] instances) => RequeueAsync(PageThrough(instances.ToList(), int.MaxValue));

    private Task<int> RequeueAsync(IAsyncEnumerable<WorkflowInstanceSummary> instances, CancellationToken cancellationToken = default) =>
        InterruptedWorkflowRequeuer.RequeueAsync(instances, _restarter, _tenantService, _tenantAccessor, _logger, cancellationToken);

    private List<int> WarningCounts() => _logger.ReceivedCalls()
        .Where(x => x.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)x.GetArguments()[0]! == LogLevel.Warning)
        .Select(x => (int)((IReadOnlyList<KeyValuePair<string, object?>>)x.GetArguments()[2]!).Single(p => p.Key == "Count").Value!)
        .ToList();

    private static async IAsyncEnumerable<WorkflowInstanceSummary> PageThrough(List<WorkflowInstanceSummary> source, int pageSize)
    {
        var offset = 0;

        while (true)
        {
            var page = source.Skip(offset).Take(pageSize).ToList();

            if (page.Count == 0)
                yield break;

            foreach (var instance in page)
                yield return instance;

            offset += pageSize;
            await Task.CompletedTask;
        }
    }

    private static WorkflowInstanceSummary Summary(string id, string? tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        DefinitionId = "definition",
        DefinitionVersionId = "definition:1",
        Version = 1,
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Interrupted,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
