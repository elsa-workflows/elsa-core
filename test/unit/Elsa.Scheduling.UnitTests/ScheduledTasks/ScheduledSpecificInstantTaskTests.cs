using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.ScheduledTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.ScheduledTasks;

/// <summary>
/// Tests for ScheduledSpecificInstantTask to ensure specific instant tasks handle edge cases correctly.
/// </summary>
public class ScheduledSpecificInstantTaskTests : IDisposable
{
    private static readonly DateTimeOffset DefaultNow = new(2025, 11, 06, 22, 50, 00, 0, TimeSpan.Zero);

    private readonly ServiceProvider _serviceProvider;
    private readonly ISystemClock _systemClock;
    private readonly ILogger<ScheduledSpecificInstantTask> _logger;
    private readonly List<ScheduledSpecificInstantTask> _tasksToDispose = new();

    public ScheduledSpecificInstantTaskTests()
    {
        var services = new ServiceCollection();
        _systemClock = Substitute.For<ISystemClock>();
        _logger = Substitute.For<ILogger<ScheduledSpecificInstantTask>>();

        services.AddSingleton(Substitute.For<ICommandSender>());
        _serviceProvider = services.BuildServiceProvider();
    }

    private ScheduledSpecificInstantTask CreateScheduledTask(
        DateTimeOffset? startAt = null,
        ISystemClock? systemClock = null)
    {
        var task = Substitute.For<ITask>();
        var scheduledTask = new ScheduledSpecificInstantTask(
            task,
            startAt ?? DefaultNow.AddMinutes(5),
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
    public void Schedule_WithZeroDelay_ShouldUseMinimumDelay()
    {
        // Arrange - simulate a case where startAt is exactly now
        SetupSystemClock(DefaultNow);
        var startAt = DefaultNow; // delay = 0

        // Act - Should adjust to 1ms minimum delay
        CreateScheduledTask(startAt: startAt);

        // Assert - Should not crash and should log warning
        AssertWarningLogged();
    }

    [Fact]
    public void Schedule_WithNegativeDelay_ShouldUseMinimumDelay()
    {
        // Arrange - simulate a case where startAt is in the past
        SetupSystemClock(DefaultNow);
        var startAt = DefaultNow.AddMinutes(-1); // Past time

        // Act - Should adjust to 1ms minimum delay
        CreateScheduledTask(startAt: startAt);

        // Assert - Should log a warning
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
        // Small delay to ensure any timer callbacks have completed
        Thread.Sleep(10);
        _serviceProvider.Dispose();
    }
}
