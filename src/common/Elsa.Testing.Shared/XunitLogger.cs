using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Testing.Shared;

public class XunitLogger(ITestOutputHelper testOutputHelper, string categoryName) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopDisposable.Instance;

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            testOutputHelper.WriteLine($"{categoryName} [{eventId}] {formatter(state, exception)}");

            if (exception != null)
                testOutputHelper.WriteLine(exception.ToString());
        }
        catch (InvalidOperationException)
        {
            // Suppress "no currently active test" exceptions that can occur when background tasks
            // (like timers) try to log after tests have completed
        }
    }

    private class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}