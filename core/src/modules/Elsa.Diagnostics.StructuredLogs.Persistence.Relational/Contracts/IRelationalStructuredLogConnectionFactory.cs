using System.Data.Common;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;

public interface IRelationalStructuredLogConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
