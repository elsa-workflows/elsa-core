using Elsa.Abstractions;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.StructuredLogs.Endpoints.StructuredLogs.Storage;

[PublicAPI]
internal class Endpoint(IEnumerable<IStructuredLogStorageDiagnostics> storageDiagnostics) : ElsaEndpointWithoutRequest<StructuredLogStorageDiagnostics>
{
    public override void Configure()
    {
        Get("/diagnostics/structured-logs/storage");
        ConfigurePermissions(StructuredLogsPermissions.Read);
    }
    
    public override Task<StructuredLogStorageDiagnostics> ExecuteAsync(CancellationToken cancellationToken)
    {
        var diagnostics = storageDiagnostics.ToList();
        var droppedWriteCount = checked(diagnostics.Aggregate(0L, (acc, x) => acc + x.DroppedWriteCount));
        return Task.FromResult(new StructuredLogStorageDiagnostics(droppedWriteCount, diagnostics.Count > 0));
    }
}
