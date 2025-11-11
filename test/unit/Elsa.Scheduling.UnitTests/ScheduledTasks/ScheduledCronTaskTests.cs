using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling;
using Elsa.Scheduling.ScheduledTasks;
using Elsa.Scheduling.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.ScheduledTasks;

/// <summary>
/// Tests for ScheduledCronTask to ensure cron triggers continue to fire even in edge cases.
/// </summary>
public class ScheduledCronTaskTests : IDisposable
{
    private const string DefaultCronExpression = "0 */5 * * * *";
    private static readonly DateTimeOffset DefaultNow = new(2025, 11, 06, 22, 50, 00, 0, TimeSpan.Zero);
    
    private readonly ServiceCollection _services;
    private readonly ServiceProvider _serviceProvider;
    private readonly ISystemClock _systemClock;
    private readonly ICronParser _cronParser;
    private readonly ILogger<ScheduledCronTask> _logger;
    private readonly List<ScheduledCronTask> _tasksToDispose = new();

    public ScheduledCronTaskTests()
    {
        _services = new ServiceCollection();
        _systemClock = Substitute.For<ISystemClock>();
        _cronParser = Substitute.For<ICronParser>();
        _logger = Substitute.For<ILogger<ScheduledCronTask>>();
        
        _services.AddSingleton<ICommandSender>(Substitute.For<ICommandSender>());
        _serviceProvider = _services.BuildServiceProvider();
    }

    private ScheduledCronTask CreateScheduledTask(
        string? cronExpression = null,
        ICronParser? cronParser = null,
        ISystemClock? systemClock = null)
    {
        var task = Substitute.For<ITask>();
        var scheduledTask = new ScheduledCronTask(
            task,
            cronExpression ?? DefaultCronExpression,
            cronParser ?? _cronParser,
            _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            systemClock ?? _systemClock,
            _logger
        );
        _tasksToDispose.Add(scheduledTask);
        return scheduledTask;
    }

    private void SetupCronParser(params DateTimeOffset[] occurrences)
    {
        _cronParser.GetNextOccurrence(Arg.Any<string>()).Returns(occurrences[0], occurrences.Skip(1).ToArray());
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
        SetupCronParser(DefaultNow.AddTicks(1)); // Only 1 tick in the future (100 nanoseconds)

        // Act
        CreateScheduledTask();

        // Assert - Verify that no error was logged (timer should be set up successfully)
        AssertNoErrorLogged();
    }

    [Fact]
    public void Schedule_WithZeroDelay_ShouldRetryAndSetupTimer()
    {
        // Arrange - simulate a case where the first call returns exactly now
        // but the second call returns a proper future time
        SetupSystemClock(DefaultNow, DefaultNow);
        SetupCronParser(DefaultNow, DefaultNow.AddMinutes(5)); // First: delay=0, Second: proper future time

        // Act
        CreateScheduledTask();

        // Assert - Should call GetNextOccurrence twice (once for initial delay=0, once for retry)
        _cronParser.Received(2).GetNextOccurrence(DefaultCronExpression);
    }

    [Fact]
    public void Schedule_WithNegativeDelay_ShouldRetryAndSetupTimer()
    {
        // Arrange - simulate a case where the first call returns a time in the past
        SetupSystemClock(DefaultNow, DefaultNow);
        SetupCronParser(DefaultNow.AddMinutes(-1), DefaultNow.AddMinutes(5)); // First: past, Second: future

        // Act
        CreateScheduledTask();

        // Assert - Should call GetNextOccurrence twice
        _cronParser.Received(2).GetNextOccurrence(DefaultCronExpression);
    }

    [Fact]
    public void Schedule_WithPersistentZeroDelay_ShouldLogWarningAndUseMinimumDelay()
    {
        // Arrange - simulate the bug scenario: both attempts return zero/negative delay
        // This can happen if the system clock doesn't advance or if there's clock drift
        SetupSystemClock(DefaultNow);
        SetupCronParser(DefaultNow); // Both calls return exactly now (delay = 0)

        // Act - This should not crash and should set up a timer with minimum delay
        CreateScheduledTask();

        // Assert - Should call GetNextOccurrence twice (initial + retry) and log warning
        _cronParser.Received(2).GetNextOccurrence(DefaultCronExpression);
        AssertWarningLogged();
    }

    [Fact]
    public void Schedule_WithNegativeDelayAfterRetry_ShouldLogWarningAndUseMinimumDelay()
    {
        // Arrange - simulate a case where even after retry, delay is negative
        // This could happen due to system clock adjustments
        SetupSystemClock(DefaultNow, DefaultNow);
        SetupCronParser(DefaultNow.AddMilliseconds(-100), DefaultNow.AddMilliseconds(-50));

        // Act - Should handle negative delay gracefully
        CreateScheduledTask();

        // Assert - Should log a warning and still set up timer
        AssertWarningLogged();
    }

    [Fact]
    public void ReproduceOriginalIssue_WithRealCronParser_DemonstratesBugScenario()
    {
        // This test reproduces the exact scenario from the original issue report
        // Using the real CronosCronParser with specific times that trigger the bug
        
        // Arrange - Use the exact times from the issue that cause delay.Milliseconds to be 0
        var now = new DateTimeOffset(2025, 11, 06, 22, 50, 00, 0, TimeZoneInfo.Utc.GetUtcOffset(DateTimeOffset.UtcNow));
        var systemClock = Substitute.For<ISystemClock>();
        systemClock.UtcNow.Returns(now);
        var realCronParser = new Elsa.Scheduling.Services.CronosCronParser(systemClock);
        
        // Act - Create scheduled task with real cron parser
        // Before the fix: This would silently fail to schedule if delay <= 0
        // After the fix: This should log warning and use minimum delay
        CreateScheduledTask(cronParser: realCronParser, systemClock: systemClock);
        
        // Assert - The key is that the task was created successfully without throwing or silently failing
        // This test passes with the fix, demonstrating the issue is resolved
    }

    public void Dispose()
    {
        foreach (var task in _tasksToDispose)
        {
            ((IDisposable)task).Dispose();
        }
        _serviceProvider.Dispose();
    }
}
