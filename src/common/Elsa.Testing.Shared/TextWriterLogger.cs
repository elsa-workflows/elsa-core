using Microsoft.Extensions.Logging;

namespace Elsa.Testing.Shared;

/// <summary>
/// Writes log messages to a framework-neutral <see cref="TextWriter"/>.
/// </summary>
public sealed class TextWriterLogger : ILogger
{
    private readonly string _categoryName;
    private readonly TextWriter _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextWriterLogger"/> class.
    /// </summary>
    public TextWriterLogger(TextWriter output, string categoryName)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(categoryName);
        _output = TextWriter.Synchronized(output);
        _categoryName = categoryName;
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopDisposable.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine($"{_categoryName} [{eventId}] {formatter(state, exception)}");

        if (exception != null)
            _output.WriteLine(exception);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
