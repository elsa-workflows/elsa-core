using Elsa.ServerLogs.Models;

namespace Elsa.ServerLogs.Contracts;

public interface IServerLogSourceRegistry
{
    event Action<ServerLogSource>? SourceChanged;

    ServerLogSource Current { get; }

    void MarkSeen(string sourceId, DateTimeOffset timestamp);

    IReadOnlyCollection<ServerLogSource> List();
}
