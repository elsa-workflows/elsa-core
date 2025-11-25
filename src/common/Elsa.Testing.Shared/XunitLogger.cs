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
        testOutputHelper.WriteLine($"{categoryName} [{eventId}] {formatter(state, exception)}");

        if (exception != null)
            testOutputHelper.WriteLine(exception.ToString());
    }

    private class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}