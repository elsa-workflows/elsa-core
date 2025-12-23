using System.Net.Sockets;

namespace Elsa.Resilience.Core.UnitTests;

public class DefaultTransientExceptionStrategyTests
{
    private readonly DefaultTransientExceptionStrategy _strategy = new();

    public static TheoryData<Type> TransientExceptionTypes => new()
    {
        typeof(HttpRequestException),
        typeof(TimeoutException),
        typeof(TaskCanceledException),
        typeof(IOException),
        typeof(SocketException),
        typeof(EndOfStreamException)
    };

    public static TheoryData<string> TransientMessagePatterns => new()
    {
        "timeout",
        "timed out",
        "connection reset",
        "connection refused",
        "broken pipe",
        "network",
        "end of stream",
        "attempted to read past the end",
        "the connection is closed",
        "connection is not open",
        "failed to connect",
        "no connection could be made",
        "an existing connection was forcibly closed",
        "TIMEOUT",
        "Connection Reset"
    };

    public static TheoryData<string> NonTransientMessagePatterns => new()
    {
        "Some random error",
        "Invalid operation",
        "Null reference"
    };

    [Theory]
    [MemberData(nameof(TransientExceptionTypes))]
    public void IsTransient_KnownTransientExceptionType_ReturnsTrue(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        Assert.True(_strategy.IsTransient(exception));
    }

    [Theory]
    [MemberData(nameof(TransientMessagePatterns))]
    public void IsTransient_ExceptionWithTransientMessagePattern_ReturnsTrue(string message)
    {
        var exception = new Exception(message);
        Assert.True(_strategy.IsTransient(exception));
    }

    [Theory]
    [MemberData(nameof(NonTransientMessagePatterns))]
    public void IsTransient_ExceptionWithNonTransientMessage_ReturnsFalse(string message)
    {
        var exception = new Exception(message);
        Assert.False(_strategy.IsTransient(exception));
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException), "Some error")]
    [InlineData(typeof(ArgumentException), "Invalid argument")]
    [InlineData(typeof(NullReferenceException), "Object reference not set")]
    public void IsTransient_NonTransientExceptionType_ReturnsFalse(Type exceptionType, string message)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;
        Assert.False(_strategy.IsTransient(exception));
    }

    [Theory]
    [InlineData(typeof(TimeoutException), true)]
    [InlineData(typeof(InvalidOperationException), false)]
    public void IsTransient_ExceptionWithEmptyMessage_ChecksTypeOnly(Type exceptionType, bool expectedResult)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "")!;
        Assert.Equal(expectedResult, _strategy.IsTransient(exception));
    }
}
