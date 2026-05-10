using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Contracts;

public interface IStructuredLogSourceRegistry
{
    event Action<StructuredLogSource>? SourceChanged;

    StructuredLogSource Current { get; }

    void MarkSeen(string sourceId, DateTimeOffset timestamp);

    IReadOnlyCollection<StructuredLogSource> List();
}
