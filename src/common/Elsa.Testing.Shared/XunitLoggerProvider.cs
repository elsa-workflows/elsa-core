using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides an <see cref="ILoggerProvider"/> implementation that writes log messages to xUnit's <see cref="ITestOutputHelper"/>.
/// </summary>
public class XunitLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new XunitLogger(testOutputHelper, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}