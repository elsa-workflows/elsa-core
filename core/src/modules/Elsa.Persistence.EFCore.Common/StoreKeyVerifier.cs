using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Elsa.Persistence.EFCore;

/// <summary>
/// Checks the live single-column primary key for provider-specific write coordination.
/// Elsa passes ASCII letters, digits, hyphens and underscores only. A verifier may return true
/// only if database-equivalent IDs of the supplied maximum length remain equal after removing
/// those separators and folding ASCII case. Returning false serializes the entire operation.
/// </summary>
public delegate Task<bool> StoreKeyVerifier(DbContext context, StoreObjectIdentifier table, string column, int maximumIdLength, CancellationToken cancellationToken);
