using System.Net.Sockets;

namespace Elsa.Resilience.Core.UnitTests.TestHelpers;

internal static class TransientExceptionTypes
{
    public static readonly Type[] KnownTransientTypes =
    [
        typeof(HttpRequestException),
        typeof(TimeoutException),
        typeof(TaskCanceledException),
        typeof(IOException),
        typeof(SocketException),
        typeof(EndOfStreamException)
    ];

    public static readonly string[] TransientMessagePatterns =
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
        "an existing connection was forcibly closed"
    ];

    public static readonly string[] NonTransientMessages =
    [
        "Some random error",
        "Invalid operation",
        "Null reference"
    ];
}
