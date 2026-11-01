namespace Elsa.Persistence.EFCore.UnitTests;

public class MigratedDatabaseRegistryTests
{
    private const string SharedDatabase = "Data Source=shared";
    private const string OwnDatabase = "Data Source=own";
    private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(10);

    private readonly MigratedDatabaseRegistry _registry = new();

    [Fact]
    public async Task MigrateOnceAsync_ForTheSameDatabaseAtTheSameTime_MigratesOnceAndOthersWait()
    {
        var migrations = 0;
        var started = NewGate();
        var release = NewGate();
        var first = _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, async _ =>
        {
            Interlocked.Increment(ref migrations);
            started.SetResult();
            await release.Task;
        });
        await started.Task.WaitAsync(SafetyTimeout);

        var others = Enumerable.Range(0, 4).Select(_ => _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, _ =>
        {
            Interlocked.Increment(ref migrations);
            return Task.CompletedTask;
        })).ToArray();

        await Task.Delay(100);
        Assert.All(others, x => Assert.False(x.IsCompleted));
        Assert.False(first.IsCompleted);

        release.SetResult();
        await Task.WhenAll(others.Append(first)).WaitAsync(SafetyTimeout);

        Assert.Equal(1, migrations);
    }

    [Fact]
    public async Task MigrateOnceAsync_AfterTheDatabaseIsMigrated_DoesNotMigrateAgain()
    {
        var migrations = 0;
        Task Migrate(CancellationToken _)
        {
            migrations++;
            return Task.CompletedTask;
        }

        await _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, Migrate);
        await _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, Migrate);

        Assert.Equal(1, migrations);
    }

    [Fact]
    public async Task MigrateOnceAsync_WhenTheFirstMigrationFails_TheNextCallerMigrates()
    {
        var started = NewGate();
        var release = NewGate();
        var first = _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, async _ =>
        {
            started.SetResult();
            await release.Task;
            throw new InvalidOperationException("Migration failed.");
        });
        await started.Task.WaitAsync(SafetyTimeout);
        var migrated = false;
        var second = _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, _ =>
        {
            migrated = true;
            return Task.CompletedTask;
        });

        release.SetResult();

        await Assert.ThrowsAsync<InvalidOperationException>(() => first);
        await second.WaitAsync(SafetyTimeout);
        Assert.True(migrated);
    }

    [Fact]
    public async Task MigrateOnceAsync_WhenAWaitingCallerIsCancelled_DoesNotMigrate()
    {
        var started = NewGate();
        var release = NewGate();
        var first = _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, async _ =>
        {
            started.SetResult();
            await release.Task;
        });
        await started.Task.WaitAsync(SafetyTimeout);
        using var cancellation = new CancellationTokenSource();
        var migrated = false;
        var second = _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, _ =>
        {
            migrated = true;
            return Task.CompletedTask;
        }, cancellation.Token);

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        release.SetResult();
        await first.WaitAsync(SafetyTimeout);
        Assert.False(migrated);
    }

    [Fact]
    public async Task MigrateOnceAsync_ForDifferentDatabasesOrDbContexts_DoesNotBlock()
    {
        var release = NewGate();
        var blocked = _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, _ => release.Task);
        var migrations = 0;
        Task Migrate(CancellationToken _)
        {
            migrations++;
            return Task.CompletedTask;
        }

        await _registry.MigrateOnceAsync(typeof(FirstDbContext), OwnDatabase, Migrate).WaitAsync(SafetyTimeout);
        await _registry.MigrateOnceAsync(typeof(SecondDbContext), SharedDatabase, Migrate).WaitAsync(SafetyTimeout);

        Assert.Equal(2, migrations);
        Assert.False(blocked.IsCompleted);
        release.SetResult();
        await blocked.WaitAsync(SafetyTimeout);
    }

    [Fact]
    public async Task MigrateOnceAsync_WhenAnOwnDatabaseIsListedFirst_SharedDatabaseCallersStillMigrateOneAtATime()
    {
        var concurrent = 0;
        var peak = 0;
        var sharedMigrations = 0;
        var firstSharedStarted = NewGate();
        var release = NewGate();
        Task MigrateShared(bool first) => _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, async _ =>
        {
            peak = Math.Max(peak, Interlocked.Increment(ref concurrent));
            Interlocked.Increment(ref sharedMigrations);

            if (first)
            {
                firstSharedStarted.SetResult();
                await release.Task;
            }

            Interlocked.Decrement(ref concurrent);
        });

        await _registry.MigrateOnceAsync(typeof(FirstDbContext), OwnDatabase, _ => Task.CompletedTask);
        var sharedFirst = MigrateShared(true);
        await firstSharedStarted.Task.WaitAsync(SafetyTimeout);
        var sharedOthers = Enumerable.Range(0, 3).Select(_ => MigrateShared(false)).ToArray();

        await Task.Delay(100);
        Assert.All(sharedOthers, x => Assert.False(x.IsCompleted));

        release.SetResult();
        await Task.WhenAll(sharedOthers.Append(sharedFirst)).WaitAsync(SafetyTimeout);

        Assert.Equal(1, peak);
        Assert.Equal(1, sharedMigrations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task MigrateOnceAsync_WithoutAConnectionString_AlwaysMigrates(string? connectionString)
    {
        var migrations = 0;
        Task Migrate(CancellationToken _)
        {
            migrations++;
            return Task.CompletedTask;
        }

        await _registry.MigrateOnceAsync(typeof(FirstDbContext), connectionString, Migrate);
        await _registry.MigrateOnceAsync(typeof(FirstDbContext), connectionString, Migrate);

        Assert.Equal(2, migrations);
    }

#pragma warning disable CS0618 // The obsolete claim API must keep coordinating with MigrateOnceAsync.
    [Fact]
    public async Task TryClaim_AfterOrDuringMigrateOnceAsync_Fails()
    {
        var started = NewGate();
        var release = NewGate();
        var migrating = _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, async _ =>
        {
            started.SetResult();
            await release.Task;
        });
        await started.Task.WaitAsync(SafetyTimeout);

        Assert.False(_registry.TryClaim(typeof(FirstDbContext), SharedDatabase));

        release.SetResult();
        await migrating.WaitAsync(SafetyTimeout);

        Assert.False(_registry.TryClaim(typeof(FirstDbContext), SharedDatabase));
    }

    [Fact]
    public async Task TryClaim_ThenRelease_LetsMigrateOnceAsyncMigrate()
    {
        var migrated = false;

        Assert.True(_registry.TryClaim(typeof(FirstDbContext), SharedDatabase));
        Assert.False(_registry.TryClaim(typeof(FirstDbContext), SharedDatabase));

        _registry.Release(typeof(FirstDbContext), SharedDatabase);
        await _registry.MigrateOnceAsync(typeof(FirstDbContext), SharedDatabase, _ =>
        {
            migrated = true;
            return Task.CompletedTask;
        });

        Assert.True(migrated);
    }

    [Fact]
    public void TryClaim_WithoutAConnectionString_AlwaysSucceeds()
    {
        Assert.True(_registry.TryClaim(typeof(FirstDbContext), null));
        Assert.True(_registry.TryClaim(typeof(FirstDbContext), null));
    }
#pragma warning restore CS0618

    private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class FirstDbContext;

    private sealed class SecondDbContext;
}
