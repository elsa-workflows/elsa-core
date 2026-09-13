using Elsa.Common;
using Elsa.Scheduling.Services;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.Services;

public class CronosCronParserTests
{
    [Test]
    [Arguments("0 0 0 * * *")] // Daily at midnight
    [Arguments("0 0 */6 * * *")] // Every 6 hours
    [Arguments("0 0 0 * * MON")] // Every Monday
    [Arguments("0 0 9 * * MON-FRI")] // Weekdays at 9 AM
    [Arguments("0 0 0 1 */3 *")] // First day of every 3 months
    [Arguments("0 0 0 L * *")] // Last day of month
    public async Task GetNextOccurrence_WithValidExpression_ReturnsTimeInFuture(string cronExpression)
    {
        // Arrange
        var parser = CreateParser(out var now);

        // Act
        var nextOccurrence = parser.GetNextOccurrence(cronExpression);

        // Assert
        await Assert.That(nextOccurrence > now).IsTrue();
    }

    [Test]
    public async Task GetNextOccurrence_WithSecondsPrecision_ReturnsCorrectSecond()
    {
        // Arrange
        var parser = CreateParser(out _);

        // Act
        var nextOccurrence = parser.GetNextOccurrence("30 * * * * *");

        // Assert
        await Assert.That(nextOccurrence.Second).IsEqualTo(30);
    }

    [Test]
    public async Task GetNextOccurrence_AtMidnight_ReturnsNextDay()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 0, 0, 0, TimeSpan.Zero);
        var parser = CreateParser(now);

        // Act
        var nextOccurrence = parser.GetNextOccurrence("0 0 0 * * *");

        // Assert
        await Assert.That(nextOccurrence.Day).IsEqualTo(7);
    }

    [Test]
    public async Task GetNextOccurrence_EverySecond_ReturnsNextSecond()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 30, 45, TimeSpan.Zero);
        var parser = CreateParser(now);

        // Act
        var nextOccurrence = parser.GetNextOccurrence("* * * * * *");

        // Assert
        await Assert.That(nextOccurrence.Second).IsEqualTo(46);
        await Assert.That((nextOccurrence - now).TotalSeconds < 2).IsTrue();
    }

    [Test]
    public async Task GetNextOccurrence_WithSpecificDayOfMonth_ReturnsCorrectDay()
    {
        // Arrange
        var parser = CreateParser(out _);

        // Act
        var nextOccurrence = parser.GetNextOccurrence("0 0 0 15 * *");

        // Assert
        await Assert.That(nextOccurrence.Day).IsEqualTo(15);
        await Assert.That(nextOccurrence.Hour).IsEqualTo(0);
    }

    [Test]
    [Arguments("invalid")]
    [Arguments("* * * *")] // Too few fields
    [Arguments("60 * * * * *")] // Invalid second (>59)
    [Arguments("* 60 * * * *")] // Invalid minute (>59)
    [Arguments("* * 25 * * *")] // Invalid hour (>23)
    public async Task GetNextOccurrence_WithInvalidExpression_ThrowsException(string cronExpression)
    {
        // Arrange
        var parser = CreateParser(out _);

        // Act & Assert
        await Assert.That(() => parser.GetNextOccurrence(cronExpression)).Throws<Exception>();
    }

    private static CronosCronParser CreateParser(out DateTimeOffset now)
    {
        now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        return CreateParser(now);
    }

    private static CronosCronParser CreateParser(DateTimeOffset now)
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        return new CronosCronParser(clock);
    }
}
