using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Core.UnitTests.Helpers;

class TestLogger : ILogger
{
    public List<(LogLevel level, EventId eventId, object state, Exception? exception, Delegate formatter)> Calls { get; } = new();
    public bool IsEnabledCalled { get; private set; }

    public bool IsEnabled(LogLevel logLevel)
    {
        IsEnabledCalled = true;
        return true;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Calls.Add((logLevel, eventId, state!, exception, formatter));
    }
}