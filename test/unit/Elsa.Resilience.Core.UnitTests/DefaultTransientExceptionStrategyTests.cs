using System.Net.Sockets;
using System.Threading.Tasks;

namespace Elsa.Resilience.Core.UnitTests;

public class DefaultTransientExceptionStrategyTests
{
    private readonly DefaultTransientExceptionStrategy _strategy = new();

    public static IEnumerable<Type> TransientExceptionTypes =>
    [
        typeof(HttpRequestException),
        typeof(TimeoutException),
        typeof(TaskCanceledException),
        typeof(IOException),
        typeof(SocketException),
        typeof(EndOfStreamException)
    ];

    public static IEnumerable<string> TransientMessagePatterns =>
    [
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
    ];

    public static IEnumerable<string> NonTransientMessagePatterns =>
    [
        "Some random error",
        "Invalid operation",
        "Null reference"
    ];

    [Test]
    [DisplayName("Known transient exception type $exceptionType should be detected as transient")]
    [MethodDataSource(nameof(TransientExceptionTypes))]
    public async Task IsTransient_KnownTransientExceptionType_ReturnsTrue(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        await Assert.That(_strategy.IsTransient(exception)).IsTrue();
    }

    [Test]
    [DisplayName("Exception message '$message' should be detected as transient")]
    [MethodDataSource(nameof(TransientMessagePatterns))]
    public async Task IsTransient_ExceptionWithTransientMessagePattern_ReturnsTrue(string message)
    {
        var exception = new Exception(message);
        await Assert.That(_strategy.IsTransient(exception)).IsTrue();
    }

    [Test]
    [DisplayName("Exception message '$message' should not be detected as transient")]
    [MethodDataSource(nameof(NonTransientMessagePatterns))]
    public async Task IsTransient_ExceptionWithNonTransientMessage_ReturnsFalse(string message)
    {
        var exception = new Exception(message);
        await Assert.That(_strategy.IsTransient(exception)).IsFalse();
    }

    [Test]
    [DisplayName("Non-transient exception type $exceptionType with '$message' should not be detected as transient")]
    [Arguments(typeof(InvalidOperationException), "Some error")]
    [Arguments(typeof(ArgumentException), "Invalid argument")]
    [Arguments(typeof(NullReferenceException), "Object reference not set")]
    public async Task IsTransient_NonTransientExceptionType_ReturnsFalse(Type exceptionType, string message)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;
        await Assert.That(_strategy.IsTransient(exception)).IsFalse();
    }

    [Test]
    [DisplayName("Exception type $exceptionType with an empty message should yield $expectedResult")]
    [Arguments(typeof(TimeoutException), true)]
    [Arguments(typeof(InvalidOperationException), false)]
    public async Task IsTransient_ExceptionWithEmptyMessage_ChecksTypeOnly(Type exceptionType, bool expectedResult)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "")!;
        await Assert.That(_strategy.IsTransient(exception)).IsEqualTo(expectedResult);
    }
}
