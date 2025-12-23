using NSubstitute;

namespace Elsa.Resilience.Core.UnitTests;

public class TransientExceptionDetectorTests
{
    private static ITransientExceptionStrategy CreateDetector(params (Exception exception, bool isTransient)[] behaviors)
    {
        var detector = Substitute.For<ITransientExceptionStrategy>();
        foreach (var (exception, isTransient) in behaviors)
            detector.IsTransient(exception).Returns(isTransient);
        return detector;
    }

    private static TransientExceptionDetector CreateService(params ITransientExceptionStrategy[] detectors) =>
        new(detectors);

    [Fact(DisplayName = "Service with no registered strategies should return false for any exception")]
    public void IsTransient_NoDetectors_ReturnsFalse()
    {
        var service = CreateService();
        var exception = new Exception("test");

        Assert.False(service.IsTransient(exception));
    }

    [Fact(DisplayName = "Service should return true when any strategy detects the exception as transient")]
    public void IsTransient_DetectorReturnsTrue_ReturnsTrue()
    {
        var exception = new Exception("test");
        var detector = CreateDetector((exception, true));
        var service = CreateService(detector);

        Assert.True(service.IsTransient(exception));
    }

    [Fact(DisplayName = "Service with multiple strategies should return true if any one detects as transient")]
    public void IsTransient_MultipleDetectorsOneReturnsTrue_ReturnsTrue()
    {
        var exception = new Exception("test");
        var detector1 = CreateDetector((exception, false));
        var detector2 = CreateDetector((exception, true));
        var service = CreateService(detector1, detector2);

        Assert.True(service.IsTransient(exception));
    }

    [Fact(DisplayName = "Service should return false when all strategies detect the exception as non-transient")]
    public void IsTransient_AllDetectorsReturnFalse_ReturnsFalse()
    {
        var exception = new Exception("test");
        var detector1 = Substitute.For<ITransientExceptionStrategy>();
        var detector2 = Substitute.For<ITransientExceptionStrategy>();
        detector1.IsTransient(Arg.Any<Exception>()).Returns(false);
        detector2.IsTransient(Arg.Any<Exception>()).Returns(false);
        var service = CreateService(detector1, detector2);

        Assert.False(service.IsTransient(exception));
    }

    [Theory(DisplayName = "Service should walk the inner exception chain to find transient exceptions")]
    [InlineData(1)] // Inner exception is transient
    [InlineData(2)] // Deep inner exception is transient
    public void IsTransient_InnerExceptionChainHasTransient_ReturnsTrue(int depth)
    {
        var transientException = new TimeoutException("timeout");
        var exception = depth switch
        {
            1 => new("outer", transientException),
            2 => new Exception("outer", new("middle", transientException)),
            _ => throw new ArgumentOutOfRangeException(nameof(depth))
        };

        var detector = Substitute.For<ITransientExceptionStrategy>();
        detector.IsTransient(transientException).Returns(true);
        detector.IsTransient(Arg.Is<Exception>(e => e != transientException)).Returns(false);
        var service = CreateService(detector);

        Assert.True(service.IsTransient(exception));
    }

    [Theory(DisplayName = "Service should inspect AggregateException inner exceptions")]
    [InlineData(1, true)]  // One transient inner exception
    [InlineData(2, false)] // No transient inner exceptions
    public void IsTransient_AggregateException_ChecksInnerExceptions(int scenario, bool expectedResult)
    {
        var detector = Substitute.For<ITransientExceptionStrategy>();

        var aggregateException = scenario switch
        {
            1 => new("aggregate", new TimeoutException("timeout")),
            2 => new AggregateException("aggregate", new InvalidOperationException("error1"), new ArgumentException("error2")),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        if (scenario == 1)
        {
            detector.IsTransient(Arg.Is<TimeoutException>(e => e.Message == "timeout")).Returns(true);
            detector.IsTransient(Arg.Is<Exception>(e => e.GetType() != typeof(TimeoutException))).Returns(false);
        }
        else
        {
            detector.IsTransient(Arg.Any<Exception>()).Returns(false);
        }

        var service = CreateService(detector);
        Assert.Equal(expectedResult, service.IsTransient(aggregateException));
    }

    [Fact(DisplayName = "AggregateException with mixed inner exceptions should be transient if any inner is transient")]
    public void IsTransient_AggregateExceptionWithMultipleInnerOneTransient_ReturnsTrue()
    {
        var transientException = new TimeoutException("timeout");
        var nonTransientException = new InvalidOperationException("invalid");
        var aggregateException = new AggregateException("aggregate", nonTransientException, transientException);

        var detector = CreateDetector(
            (aggregateException, false),
            (nonTransientException, false),
            (transientException, true));
        var service = CreateService(detector);

        Assert.True(service.IsTransient(aggregateException));
    }
}
