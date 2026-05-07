using Elsa.Diagnostics.Models;

namespace Elsa.Diagnostics.Contracts;

public interface IServerLogSourceRegistry
{
    ServerLogSource Current { get; }
    
    void MarkSeen(string sourceId, DateTimeOffset timestamp);
    
    IReadOnlyCollection<ServerLogSource> List();
}
