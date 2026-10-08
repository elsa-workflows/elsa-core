using Elsa.Workflows.Admission.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Slack.SocketMode.Persistence;

internal sealed class PostgreSqlSlackSocketIdentityConflictReader(SlackSocketReceiptTransactions transactions) : IAdmissionIdentityConflictReader
{
    public async Task<bool> HasConflictAsync(AdmissionElsaDbContext transactionContext, string identityHash, CancellationToken cancellationToken = default)
    {
        await using var receipts = await transactions.EnlistAsync(transactionContext, cancellationToken);
        // Global identity lookup deliberately does not conceal a foreign-scope reservation.
        return await receipts.Receipts.AnyAsync(x => x.IdentityHash == identityHash, cancellationToken);
    }
}
