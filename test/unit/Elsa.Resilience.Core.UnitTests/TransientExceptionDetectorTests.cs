using NSubstitute;

namespace Elsa.Resilience.Core.UnitTests;

public class TransientExceptionDetectorTests
{
    [Fact(DisplayName = "Service with no registered strategies should return false for any exception")]
    public void IsTransient_NoDetectors_ReturnsFalse()
    {
        var detector = CreateDetector();
        var exception = new Exception("test");

        Assert.False(detector.IsTransient(exception));
    }

    [Fact(DisplayName = "Service should return true when any strategy detects the exception as transient")]
    public void IsTransient_DetectorReturnsTrue_ReturnsTrue()
    {
        var exception = new Exception("test");
        var strategy = CreateStrategy((exception, true));
        var detector = CreateDetector(strategy);

        Assert.True(detector.IsTransient(exception));
    }

    [Fact(DisplayName = "Service with multiple strategies should return true if any one detects as transient")]
    public void IsTransient_MultipleDetectorsOneReturnsTrue_ReturnsTrue()
    {
        var exception = new Exception("test");
        var strategy1 = CreateStrategy((exception, false));
        var strategy2 = CreateStrategy((exception, true));
        var detector = CreateDetector(strategy1, strategy2);

        Assert.True(detector.IsTransient(exception));
    }

    [Fact(DisplayName = "Service should return false when all strategies detect the exception as non-transient")]
    public void IsTransient_AllDetectorsReturnFalse_ReturnsFalse()
    {
        var exception = new Exception("test");
        var strategy1 = Substitute.For<ITransientExceptionStrategy>();
        var strategy2 = Substitute.For<ITransientExceptionStrategy>();
        strategy1.IsTransient(Arg.Any<Exception>()).Returns(false);
        strategy2.IsTransient(Arg.Any<Exception>()).Returns(false);
        var detector = CreateDetector(strategy1, strategy2);

        Assert.False(detector.IsTransient(exception));
    }

    [Theory(DisplayName = "Service should walk the inner exception chain to find transient exceptions")]
    [MemberData(nameof(InnerExceptionChainTestCases))]
    public void IsTransient_InnerExceptionChainHasTransient_ReturnsTrue(Exception exception, Exception transientException)
    {
        var strategy = Substitute.For<ITransientExceptionStrategy>();
        strategy.IsTransient(transientException).Returns(true);
        strategy.IsTransient(Arg.Is<Exception>(e => e != transientException)).Returns(false);
        var detector = CreateDetector(strategy);

        Assert.True(detector.IsTransient(exception));
    }

    [Theory(DisplayName = "Service should inspect AggregateException inner exceptions")]
    [MemberData(nameof(AggregateExceptionTestCases))]
    public void IsTransient_AggregateException_ChecksInnerExceptions(
        AggregateException aggregateException,
        Action<ITransientExceptionStrategy> configureDetector,
        bool expectedResult)
    {
        var strategy = Substitute.For<ITransientExceptionStrategy>();
        configureDetector(strategy);
        var detector = CreateDetector(strategy);

        Assert.Equal(expectedResult, detector.IsTransient(aggregateException));
    }

    [Fact(DisplayName = "AggregateException with mixed inner exceptions should be transient if any inner is transient")]
    public void IsTransient_AggregateExceptionWithMultipleInnerOneTransient_ReturnsTrue()
    {
        var transientException = new TimeoutException("timeout");
        var nonTransientException = new InvalidOperationException("invalid");
        var aggregateException = new AggregateException("aggregate", nonTransientException, transientException);

        var strategy = CreateStrategy(
            (aggregateException, false),
            (nonTransientException, false),
            (transientException, true));
        var detector = CreateDetector(strategy);

        Assert.True(detector.IsTransient(aggregateException));
    }

    public static IEnumerable<object[]> InnerExceptionChainTestCases
    {
        get
        {
            var transientException = new TimeoutException("timeout");

            yield return
            [
                new Exception("outer", transientException),
                transientException
            ];

            yield return
            [
                new Exception("outer", new("middle", transientException)),
                transientException
            ];
        }
    }

    public static IEnumerable<object[]> AggregateExceptionTestCases
    {
        get
        {
            // Scenario 1: One transient inner exception
            var timeoutException = new TimeoutException("timeout");
            yield return
            [
                new AggregateException("aggregate", timeoutException),
                (Action<ITransientExceptionStrategy>)(detector =>
                {
                    detector.IsTransient(Arg.Is<TimeoutException>(e => e.Message == "timeout")).Returns(true);
                    detector.IsTransient(Arg.Is<Exception>(e => e.GetType() != typeof(TimeoutException))).Returns(false);
                }),
                true
            ];

            // Scenario 2: No transient inner exceptions
            yield return
            [
                new AggregateException("aggregate", new InvalidOperationException("error1"), new ArgumentException("error2")),
                (Action<ITransientExceptionStrategy>)(detector =>
                {
                    detector.IsTransient(Arg.Any<Exception>()).Returns(false);
                }),
                false
            ];
        }
    }

    private static ITransientExceptionStrategy CreateStrategy(params (Exception exception, bool isTransient)[] behaviors)
    {
        var detector = Substitute.For<ITransientExceptionStrategy>();
        foreach (var (exception, isTransient) in behaviors)
            detector.IsTransient(exception).Returns(isTransient);
        return detector;
    }

    private static TransientExceptionDetector CreateDetector(params ITransientExceptionStrategy[] detectors) => new(detectors);
}
