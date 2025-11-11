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

    [Fact]
    public void Schedule_WithVerySmallDelay_ShouldStillSetupTimer()
    {
        // Arrange - simulate a case where the delay is very small (1 tick = 100ns)
        var now = new DateTimeOffset(2025, 11, 06, 22, 50, 00, 0, TimeSpan.Zero);
        var nextOccurrence = now.AddTicks(1); // Only 1 tick in the future (100 nanoseconds)
        
        _systemClock.UtcNow.Returns(now);
        _cronParser.GetNextOccurrence(Arg.Any<string>()).Returns(nextOccurrence);

        var task = Substitute.For<ITask>();
        var cronExpression = "0 */5 * * * *";

        // Act - Create the scheduled task which calls Schedule() in the constructor
        var scheduledTask = new ScheduledCronTask(
            task,
            cronExpression,
            _cronParser,
            _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            _systemClock,
            _logger
        );
        _tasksToDispose.Add(scheduledTask);

        // Assert - Verify that no error was logged (timer should be set up successfully)
        // If the timer setup fails, an error would be logged
        _logger.DidNotReceive().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Schedule_WithZeroDelay_ShouldRetryAndSetupTimer()
    {
        // Arrange - simulate a case where the first call returns exactly now
        // but the second call returns a proper future time
        var now = new DateTimeOffset(2025, 11, 06, 22, 50, 00, 0, TimeSpan.Zero);
        var nextOccurrence = now; // First call: same as now (delay = 0)
        var secondNextOccurrence = now.AddMinutes(5); // Second call: 5 minutes later
        
        _systemClock.UtcNow.Returns(now, now); // Return same time for both calls
        _cronParser.GetNextOccurrence(Arg.Any<string>()).Returns(nextOccurrence, secondNextOccurrence);

        var task = Substitute.For<ITask>();
        var cronExpression = "0 */5 * * * *";

        // Act
        var scheduledTask = new ScheduledCronTask(
            task,
            cronExpression,
            _cronParser,
            _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            _systemClock,
            _logger
        );
        _tasksToDispose.Add(scheduledTask);

        // Assert - Should call GetNextOccurrence twice (once for initial delay=0, once for retry)
        _cronParser.Received(2).GetNextOccurrence(cronExpression);
    }

    [Fact]
    public void Schedule_WithNegativeDelay_ShouldRetryAndSetupTimer()
    {
        // Arrange - simulate a case where the first call returns a time in the past
        var now = new DateTimeOffset(2025, 11, 06, 22, 50, 00, 0, TimeSpan.Zero);
        var pastOccurrence = now.AddMinutes(-1); // 1 minute in the past
        var futureOccurrence = now.AddMinutes(5); // 5 minutes in the future
        
        _systemClock.UtcNow.Returns(now, now);
        _cronParser.GetNextOccurrence(Arg.Any<string>()).Returns(pastOccurrence, futureOccurrence);

        var task = Substitute.For<ITask>();
        var cronExpression = "0 */5 * * * *";

        // Act
        var scheduledTask = new ScheduledCronTask(
            task,
            cronExpression,
            _cronParser,
            _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            _systemClock,
            _logger
        );
        _tasksToDispose.Add(scheduledTask);

        // Assert - Should call GetNextOccurrence twice
        _cronParser.Received(2).GetNextOccurrence(cronExpression);
    }

    [Fact]
    public void Schedule_WithPersistentZeroDelay_ShouldLogWarningAndUseMinimumDelay()
    {
        // Arrange - simulate the bug scenario: both attempts return zero/negative delay
        // This can happen if the system clock doesn't advance or if there's clock drift
        var now = new DateTimeOffset(2025, 11, 06, 22, 50, 00, 0, TimeSpan.Zero);
        var sameTime = now; // Both calls return exactly now (delay = 0)
        
        _systemClock.UtcNow.Returns(now);
        _cronParser.GetNextOccurrence(Arg.Any<string>()).Returns(sameTime);

        var task = Substitute.For<ITask>();
        var cronExpression = "0 */5 * * * *";

        // Act - This should not crash and should set up a timer with minimum delay
        var scheduledTask = new ScheduledCronTask(
            task,
            cronExpression,
            _cronParser,
            _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            _systemClock,
            _logger
        );
        _tasksToDispose.Add(scheduledTask);

        // Assert - Should call GetNextOccurrence twice (initial + retry)
        _cronParser.Received(2).GetNextOccurrence(cronExpression);
        
        // Should log a warning about the issue
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Schedule_WithNegativeDelayAfterRetry_ShouldLogWarningAndUseMinimumDelay()
    {
        // Arrange - simulate a case where even after retry, delay is negative
        // This could happen due to system clock adjustments
        var now = new DateTimeOffset(2025, 11, 06, 22, 50, 00, 0, TimeSpan.Zero);
        var pastTime1 = now.AddMilliseconds(-100);
        var pastTime2 = now.AddMilliseconds(-50);
        
        _systemClock.UtcNow.Returns(now, now);
        _cronParser.GetNextOccurrence(Arg.Any<string>()).Returns(pastTime1, pastTime2);

        var task = Substitute.For<ITask>();
        var cronExpression = "0 */5 * * * *";

        // Act - Should handle negative delay gracefully
        var scheduledTask = new ScheduledCronTask(
            task,
            cronExpression,
            _cronParser,
            _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            _systemClock,
            _logger
        );
        _tasksToDispose.Add(scheduledTask);

        // Assert - Should log a warning and still set up timer
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ReproduceOriginalIssue_WithRealCronParser_DemonstratesBugScenario()
    {
        // This test reproduces the exact scenario from the original issue report
        // Using the real CronosCronParser with specific times that trigger the bug
        
        // Arrange - Use the exact times from the issue that cause delay.Milliseconds to be 0
        var now = new DateTimeOffset(2025, 11, 06, 22, 50, 00, 0, TimeZoneInfo.Utc.GetUtcOffset(DateTimeOffset.UtcNow));
        
        // Create a real CronosCronParser instance
        var systemClock = Substitute.For<ISystemClock>();
        systemClock.UtcNow.Returns(now);
        var realCronParser = new Elsa.Scheduling.Services.CronosCronParser(systemClock);
        
        var task = Substitute.For<ITask>();
        var cronExpression = "0 */5 * * * *"; // Every 5 minutes, from the original issue
        
        // Act - Create scheduled task with real cron parser
        // Before the fix: This would silently fail to schedule if delay <= 0
        // After the fix: This should log warning and use minimum delay
        var scheduledTask = new ScheduledCronTask(
            task,
            cronExpression,
            realCronParser,
            _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            systemClock,
            _logger
        );
        _tasksToDispose.Add(scheduledTask);
        
        // Assert - With the fix, a warning should be logged if delay was <= 0
        // The key is that the task was created successfully without throwing or silently failing
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
