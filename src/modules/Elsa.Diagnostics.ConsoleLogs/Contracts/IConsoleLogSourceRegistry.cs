namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

public interface IConsoleLogSourceRegistry
{
    event Action<ConsoleLogSource>? SourceChanged;

    ConsoleLogSource Current { get; }

    void MarkSeen(string sourceId, DateTimeOffset timestamp);

    IReadOnlyCollection<ConsoleLogSource> List();
}
