using Elsa.Common;
using Elsa.Scheduling.Services;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.Services;

public class CronosCronParserTests
{
    [Theory]
    [InlineData("0 0 0 * * *")] // Daily at midnight
    [InlineData("0 0 */6 * * *")] // Every 6 hours
    [InlineData("0 0 0 * * MON")] // Every Monday
    [InlineData("0 0 9 * * MON-FRI")] // Weekdays at 9 AM
    [InlineData("0 0 0 1 */3 *")] // First day of every 3 months
    [InlineData("0 0 0 L * *")] // Last day of month
    public void GetNextOccurrence_WithValidExpression_ReturnsTimeInFuture(string cronExpression)
    {
        // Arrange
        var parser = CreateParser(out var now);

        // Act
        var nextOccurrence = parser.GetNextOccurrence(cronExpression);

        // Assert
        Assert.True(nextOccurrence > now);
    }

    [Fact]
    public void GetNextOccurrence_WithSecondsPrecision_ReturnsCorrectSecond()
    {
        // Arrange
        var parser = CreateParser(out _);

        // Act
        var nextOccurrence = parser.GetNextOccurrence("30 * * * * *");

        // Assert
        Assert.Equal(30, nextOccurrence.Second);
    }

    [Fact]
    public void GetNextOccurrence_AtMidnight_ReturnsNextDay()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 0, 0, 0, TimeSpan.Zero);
        var parser = CreateParser(now);

        // Act
        var nextOccurrence = parser.GetNextOccurrence("0 0 0 * * *");

        // Assert
        Assert.Equal(7, nextOccurrence.Day);
    }

    [Fact]
    public void GetNextOccurrence_EverySecond_ReturnsNextSecond()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 30, 45, TimeSpan.Zero);
        var parser = CreateParser(now);

        // Act
        var nextOccurrence = parser.GetNextOccurrence("* * * * * *");

        // Assert
        Assert.Equal(46, nextOccurrence.Second);
        Assert.True((nextOccurrence - now).TotalSeconds < 2);
    }

    [Fact]
    public void GetNextOccurrence_WithSpecificDayOfMonth_ReturnsCorrectDay()
    {
        // Arrange
        var parser = CreateParser(out _);

        // Act
        var nextOccurrence = parser.GetNextOccurrence("0 0 0 15 * *");

        // Assert
        Assert.Equal(15, nextOccurrence.Day);
        Assert.Equal(0, nextOccurrence.Hour);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("* * * *")] // Too few fields
    [InlineData("60 * * * * *")] // Invalid second (>59)
    [InlineData("* 60 * * * *")] // Invalid minute (>59)
    [InlineData("* * 25 * * *")] // Invalid hour (>23)
    public void GetNextOccurrence_WithInvalidExpression_ThrowsException(string cronExpression)
    {
        // Arrange
        var parser = CreateParser(out _);

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => parser.GetNextOccurrence(cronExpression));
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
