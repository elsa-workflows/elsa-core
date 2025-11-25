using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.ScheduledTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.ScheduledTasks;

/// <summary>
/// Tests for ScheduledRecurringTask to ensure recurring tasks handle edge cases correctly.
/// </summary>
public class ScheduledRecurringTaskTests : IDisposable
{
    private static readonly DateTimeOffset DefaultNow = new(2025, 11, 06, 22, 50, 00, 0, TimeSpan.Zero);
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

    private readonly ServiceProvider _serviceProvider;
    private readonly ISystemClock _systemClock;
    private readonly ILogger<ScheduledRecurringTask> _logger;
    private readonly List<ScheduledRecurringTask> _tasksToDispose = new();

    public ScheduledRecurringTaskTests()
    {
        var services = new ServiceCollection();
        _systemClock = Substitute.For<ISystemClock>();
        _logger = Substitute.For<ILogger<ScheduledRecurringTask>>();

        services.AddSingleton(Substitute.For<ICommandSender>());
        _serviceProvider = services.BuildServiceProvider();
    }

    private ScheduledRecurringTask CreateScheduledTask(
        DateTimeOffset? startAt = null,
        TimeSpan? interval = null,
        ISystemClock? systemClock = null)
    {
        var task = Substitute.For<ITask>();
        var scheduledTask = new ScheduledRecurringTask(
            task,
            startAt ?? DefaultNow.AddMinutes(5),
            interval ?? DefaultInterval,
            systemClock ?? _systemClock,
            _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            _logger
        );
        _tasksToDispose.Add(scheduledTask);
        return scheduledTask;
    }

    private void SetupSystemClock(params DateTimeOffset[] times)
    {
        _systemClock.UtcNow.Returns(times[0], times.Skip(1).ToArray());
    }

    private void AssertNoErrorLogged()
    {
        _logger.DidNotReceive().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private void AssertWarningLogged(int expectedCount = 1)
    {
        _logger.Received(expectedCount).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Schedule_WithVerySmallDelay_ShouldStillSetupTimer()
    {
        // Arrange - simulate a case where the delay is very small (1 tick = 100ns)
        SetupSystemClock(DefaultNow);
        var startAt = DefaultNow.AddTicks(1); // Only 1 tick in the future (100 nanoseconds)

        // Act
        CreateScheduledTask(startAt: startAt);

        // Assert - Verify that no error was logged (timer should be set up successfully)
        AssertNoErrorLogged();
    }

    [Fact]
    public void Schedule_WithZeroDelay_ShouldRetryAndSetupTimer()
    {
        // Arrange - simulate a case where the first call returns exactly now
        // but the second call returns a proper future time
        SetupSystemClock(DefaultNow, DefaultNow);
        var startAt = DefaultNow; // First: delay=0

        // Act
        var task = CreateScheduledTask(startAt: startAt);

        // Dispose immediately to prevent timer from firing
        ((IDisposable)task).Dispose();

        // Assert - System clock should be called at least twice (initial + retry)
        // May be called more if timer fires before disposal in rare race conditions
        _ = _systemClock.Received().UtcNow;
        var calls = _systemClock.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "get_UtcNow");
        Assert.True(calls >= 2, $"Expected at least 2 calls to UtcNow, but got {calls}");
    }

    [Fact]
    public void Schedule_WithNegativeDelay_ShouldRetryAndSetupTimer()
    {
        // Arrange - simulate a case where the first call returns a time in the past
        SetupSystemClock(DefaultNow, DefaultNow);
        var startAt = DefaultNow.AddMinutes(-1); // Past time

        // Act
        var task = CreateScheduledTask(startAt: startAt);

        // Dispose immediately to prevent timer from firing
        ((IDisposable)task).Dispose();

        // Assert - System clock should be called at least twice (initial + retry)
        // May be called more if timer fires before disposal in rare race conditions
        _ = _systemClock.Received().UtcNow;
        var calls = _systemClock.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "get_UtcNow");
        Assert.True(calls >= 2, $"Expected at least 2 calls to UtcNow, but got {calls}");
    }

    [Fact]
    public void Schedule_WithPersistentZeroDelay_ShouldLogWarningAndUseMinimumDelay()
    {
        // Arrange - simulate the bug scenario: both attempts return zero/negative delay
        // This can happen if the system clock doesn't advance or if there's clock drift
        SetupSystemClock(DefaultNow);
        var startAt = DefaultNow; // Both calls return exactly now (delay = 0)

        // Act - This should not crash and should set up a timer with minimum delay
        var task = CreateScheduledTask(startAt: startAt);

        // Dispose immediately to prevent timer from firing and recursing
        ((IDisposable)task).Dispose();
        Thread.Sleep(5); // Brief wait to ensure disposal completes

        // Assert - Should call UtcNow at least twice (initial + retry)
        // May be called more if timer fires before disposal
        _ = _systemClock.Received().UtcNow;
        var calls = _systemClock.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "get_UtcNow");
        Assert.True(calls >= 2, $"Expected at least 2 calls to UtcNow, but got {calls}");
        AssertWarningLogged();
    }

    [Fact]
    public void Schedule_WithNegativeDelayAfterRetry_ShouldLogWarningAndUseMinimumDelay()
    {
        // Arrange - simulate a case where even after retry, delay is negative
        // This could happen due to system clock adjustments
        SetupSystemClock(DefaultNow, DefaultNow);
        var startAt = DefaultNow.AddMilliseconds(-100); // Negative delay

        // Act - Should handle negative delay gracefully
        var task = CreateScheduledTask(startAt: startAt);

        // Dispose immediately to prevent timer from firing
        ((IDisposable)task).Dispose();

        // Assert - Should log a warning and still set up timer
        AssertWarningLogged();
    }

    [Fact]
    public void DisposeDuringTimerCallback_ShouldNotCrash()
    {
        // Arrange - set up a very short delay so timer fires quickly
        SetupSystemClock(DefaultNow);
        var startAt = DefaultNow; // Will use 1ms minimum delay

        // Act - Create task and immediately dispose it (simulating race condition)
        var task = CreateScheduledTask(startAt: startAt);
        Thread.Sleep(5); // Give timer a chance to start firing
        ((IDisposable)task).Dispose();
        Thread.Sleep(10); // Give any in-flight callbacks time to complete

        // Assert - Should not crash (implicit - test passes if no exception thrown)
    }

    public void Dispose()
    {
        // Dispose tasks first to stop timers before disposing ServiceProvider
        foreach (var task in _tasksToDispose)
        {
            ((IDisposable)task).Dispose();
        }
        _serviceProvider.Dispose();
    }
}
