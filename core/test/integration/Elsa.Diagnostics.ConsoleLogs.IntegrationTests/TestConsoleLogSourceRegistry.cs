using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Models;

namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

internal sealed class TestConsoleLogSourceRegistry : IConsoleLogSourceRegistry
{
    public event Action<ConsoleLogSource>? SourceChanged;

    public ConsoleLogSource Current { get; private set; } = new() { Id = "test-source" };

    public ConsoleLogSource MarkSeen(ConsoleLogSource source, DateTimeOffset timestamp)
    {
        Current = source;
        SourceChanged?.Invoke(source);
        return source;
    }

    public IReadOnlyCollection<ConsoleLogSource> List()
    {
        return [Current];
    }

    public void Raise(ConsoleLogSource source)
    {
        SourceChanged?.Invoke(source);
    }
}
