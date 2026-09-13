using Microsoft.Extensions.Logging;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides loggers that write to a framework-neutral <see cref="TextWriter"/>.
/// </summary>
public sealed class TextWriterLoggerProvider : ILoggerProvider
{
    private readonly TextWriter _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextWriterLoggerProvider"/> class.
    /// </summary>
    public TextWriterLoggerProvider(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        _output = TextWriter.Synchronized(output);
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new TextWriterLogger(_output, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
