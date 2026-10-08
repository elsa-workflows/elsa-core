using Elsa.Slack.SocketMode.Persistence;
using Elsa.Workflows.Admission;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Slack.Tests.Persistence;

public sealed class SlackSocketReceiptCleanupDriverTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task IneligiblePagesAdvanceAndACompletedScanRestartsForEarlierInsertions()
    {
        await using var fixture = new CleanupFixture();
        fixture.Candidates.AddRange([new("a", 7), new("b", 8), new("c", 9)]);
        Assert.Equal(new SlackSocketReceiptCleanupBatch(2, 0, false), await fixture.Driver.RunBatchAsync(2, Now));
        Assert.Equal(new SlackSocketReceiptCleanupBatch(1, 0, true), await fixture.Driver.RunBatchAsync(2, Now));
        fixture.Candidates.Insert(0, new("0-later-insertion", 10));
        Assert.Equal(new SlackSocketReceiptCleanupBatch(2, 0, false), await fixture.Driver.RunBatchAsync(2, Now));
        Assert.Equal(new string?[] { null, "b", null }, fixture.Reads.Select(x => x.Cursor));
        Assert.Equal(new[] { "a", "b", "c", "0-later-insertion", "a" }, fixture.Cleanups.Select(x => x.Id));
        Assert.All(fixture.Cleanups, call =>
        {
            Assert.Equal(Now, call.Now);
            Assert.Equal(CleanupFixture.Authority, call.Authority);
        });
        Assert.Equal(7, fixture.Cleanups[0].Revision);
        Assert.Equal(8, fixture.Cleanups[1].Revision);
        Assert.Equal(3, fixture.Created);
        Assert.Equal(3, fixture.Disposed);
    }

    [Fact]
    public async Task CallerOwnedStoreRetainsItsScopeAndSharesTheSameFiniteCursor()
    {
        await using var fixture = new CleanupFixture();
        fixture.Candidates.AddRange([new("a", 7), new("b", 8)]);
        await using (var scope = fixture.Scopes.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ISlackSocketDiscardStore>();
            Assert.Equal(new SlackSocketReceiptCleanupBatch(1, 0, false),
                await fixture.Driver.RunBatchAsync(store, 1, Now, CancellationToken.None));
            Assert.Equal(1, fixture.Created);
            Assert.Equal(0, fixture.Disposed);
        }
        Assert.Equal(1, fixture.Disposed);
        Assert.Equal(new SlackSocketReceiptCleanupBatch(1, 0, false), await fixture.Driver.RunBatchAsync(1, Now));
        Assert.Equal(new string?[] { null, "a" }, fixture.Reads.Select(x => x.Cursor));
        Assert.Equal(new[] { "a", "b" }, fixture.Cleanups.Select(x => x.Id));
        Assert.Equal(2, fixture.Created);
        Assert.Equal(2, fixture.Disposed);
    }

    [Fact]
    public async Task AnExactFullLastPageCompletesOnTheNextEmptyBatch()
    {
        await using var fixture = new CleanupFixture();
        fixture.Candidates.AddRange([new("a", 1), new("b", 2)]);
        fixture.Cleanup = (_, _, _, _, _) => Task.FromResult(true);
        Assert.Equal(new SlackSocketReceiptCleanupBatch(2, 2, false), await fixture.Driver.RunBatchAsync(2, Now));
        Assert.Equal(new SlackSocketReceiptCleanupBatch(0, 0, true), await fixture.Driver.RunBatchAsync(2, Now));
        fixture.Candidates.Add(new("0-new", 3));
        Assert.Equal(new SlackSocketReceiptCleanupBatch(1, 1, true), await fixture.Driver.RunBatchAsync(2, Now));
        Assert.Equal(new string?[] { null, "b", null }, fixture.Reads.Select(x => x.Cursor));
        Assert.Equal(3, fixture.Cleanups.Count);
    }

    [Fact]
    public async Task FailedCandidateDoesNotAdvanceWhileEarlierDefiniteWorkRemainsAdvanced()
    {
        await using var fixture = new CleanupFixture();
        fixture.Candidates.AddRange([new("a", 1), new("b", 2), new("c", 3)]);
        var fail = true;
        fixture.Cleanup = (id, _, _, _, _) =>
        {
            if (id == "b" && fail)
            {
                fail = false;
                throw new InvalidOperationException("fixture-cleanup-uncertain");
            }
            return Task.FromResult(id != "b");
        };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Driver.RunBatchAsync(3, Now));
        Assert.Equal("fixture-cleanup-uncertain", error.Message);
        Assert.Equal(new SlackSocketReceiptCleanupBatch(2, 1, true), await fixture.Driver.RunBatchAsync(3, Now));
        Assert.Equal(new string?[] { null, "a" }, fixture.Reads.Select(x => x.Cursor));
        Assert.Equal(new[] { "a", "b", "b", "c" }, fixture.Cleanups.Select(x => x.Id));
        Assert.Single(fixture.Candidates, x => x.Id == "b");
        Assert.Equal(2, fixture.Disposed);
    }

    [Fact]
    public async Task OverlappingBatchesRejectWithoutCreatingAnotherScopeOrQueue()
    {
        await using var fixture = new CleanupFixture();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Find = async (_, _, cancellationToken) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            return [];
        };
        var first = fixture.Driver.RunBatchAsync(1, Now);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Driver.RunBatchAsync(1, Now));
            Assert.Equal(1, fixture.Created);
        }
        finally
        {
            release.TrySetResult();
            await first;
        }
        Assert.Equal(new SlackSocketReceiptCleanupBatch(0, 0, true), first.Result);
        Assert.Equal(1, fixture.Disposed);
    }

    [Fact]
    public async Task CancellationAndInvalidBoundsCannotPerformCleanupAndDoNotPinTheDriver()
    {
        await using var fixture = new CleanupFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.RunBatchAsync(1, Now, cancellation.Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Driver.RunBatchAsync(0, Now));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Driver.RunBatchAsync(1001, Now));
        Assert.Equal(0, fixture.Created);
        using var duringLookup = new CancellationTokenSource();
        fixture.Find = (_, _, _) =>
        {
            duringLookup.Cancel();
            return Task.FromResult<IReadOnlyList<SlackSocketDiscardCleanupCandidate>>([new("a", 1)]);
        };
        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Driver.RunBatchAsync(1, Now, duringLookup.Token));
        Assert.Empty(fixture.Cleanups);
        fixture.Find = (_, _, _) => Task.FromResult<IReadOnlyList<SlackSocketDiscardCleanupCandidate>>([]);
        Assert.Equal(new SlackSocketReceiptCleanupBatch(0, 0, true), await fixture.Driver.RunBatchAsync(1, Now));
        Assert.Equal(2, fixture.Disposed);
        Assert.Throws<ArgumentException>(() => new SlackSocketReceiptCleanupDriver(fixture.Scopes, ""));
        Assert.Throws<ArgumentException>(() => new SlackSocketReceiptCleanupDriver(fixture.Scopes, new string('x', AdmissionLimits.AuthorityBytes + 1)));
    }

    [Fact]
    public async Task AStoreReturningAnOversizedPageFailsBeforeAnyCleanup()
    {
        await using var fixture = new CleanupFixture();
        fixture.Find = (_, _, _) => Task.FromResult<IReadOnlyList<SlackSocketDiscardCleanupCandidate>>([new("a", 1), new("b", 1)]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Driver.RunBatchAsync(1, Now));
        Assert.Empty(fixture.Cleanups);
        Assert.Equal(1, fixture.Disposed);
    }

    private sealed class CleanupFixture : IAsyncDisposable
    {
        internal const string Authority = "synthetic-cleanup-authority";
        private readonly ServiceProvider _services;
        internal IServiceScopeFactory Scopes => _services.GetRequiredService<IServiceScopeFactory>();
        internal SlackSocketReceiptCleanupDriver Driver { get; }
        internal List<SlackSocketDiscardCleanupCandidate> Candidates { get; } = [];
        internal List<(int Limit, string? Cursor)> Reads { get; } = [];
        internal List<(string Id, long Revision, DateTimeOffset Now, string Authority)> Cleanups { get; } = [];
        internal Func<int, string?, CancellationToken, Task<IReadOnlyList<SlackSocketDiscardCleanupCandidate>>> Find { get; set; }
        internal Func<string, long, DateTimeOffset, string, CancellationToken, Task<bool>> Cleanup { get; set; } = (_, _, _, _, _) => Task.FromResult(false);
        internal int Created { get; private set; }
        internal int Disposed { get; private set; }

        internal CleanupFixture()
        {
            Find = (limit, cursor, _) => Task.FromResult<IReadOnlyList<SlackSocketDiscardCleanupCandidate>>(
                Candidates.Where(x => cursor == null || string.CompareOrdinal(x.Id, cursor) > 0).OrderBy(x => x.Id, StringComparer.Ordinal).Take(limit).ToArray());
            var services = new ServiceCollection();
            services.AddScoped<ISlackSocketDiscardStore>(_ =>
            {
                Created++;
                return new CleanupStore(this);
            });
            _services = services.BuildServiceProvider();
            Driver = new(Scopes, Authority);
        }

        public ValueTask DisposeAsync() => _services.DisposeAsync();

        private sealed class CleanupStore(CleanupFixture fixture) : ISlackSocketDiscardStore, IAsyncDisposable
        {
            public Task ValidateProvisioningAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<SlackSocketDiscardResult> RecordDiscardAsync(SlackSocketDiscardRequest request, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<IReadOnlyList<SlackSocketDiscardCleanupCandidate>> FindCleanupCandidatesAsync(int limit, string? afterId, CancellationToken cancellationToken = default)
            {
                fixture.Reads.Add((limit, afterId));
                return fixture.Find(limit, afterId, cancellationToken);
            }

            public async Task<bool> CleanupAsync(string receiptId, long revision, DateTimeOffset now, string authority, CancellationToken cancellationToken = default)
            {
                fixture.Cleanups.Add((receiptId, revision, now, authority));
                var removed = await fixture.Cleanup(receiptId, revision, now, authority, cancellationToken);
                if (removed)
                {
                    fixture.Candidates.RemoveAll(x => x.Id == receiptId);
                }
                return removed;
            }

            public ValueTask DisposeAsync()
            {
                fixture.Disposed++;
                return ValueTask.CompletedTask;
            }
        }
    }
}
