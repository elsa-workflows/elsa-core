using Elsa.Workflows.Admission.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Elsa.Slack.SocketMode.Persistence;

internal sealed class SlackSocketReceiptTransactions(IDbContextFactory<SlackSocketReceiptElsaDbContext> factory)
{
    internal async Task<SlackSocketReceiptElsaDbContext> EnlistAsync(AdmissionElsaDbContext admission, CancellationToken cancellationToken)
    {
        var transaction = admission.Database.CurrentTransaction ?? throw new InvalidOperationException("socket_receipt_transaction_required");
        var receipt = await factory.CreateDbContextAsync(cancellationToken);
        try
        {
            DemandProvider(admission);
            DemandProvider(receipt);
            var admissionConnection = admission.Database.GetDbConnection();
            var configured = new NpgsqlConnectionStringBuilder(receipt.Database.GetDbConnection().ConnectionString);
            var selected = new NpgsqlConnectionStringBuilder(admissionConnection.ConnectionString);
            if (configured.Host != selected.Host || configured.Port != selected.Port || configured.Database != selected.Database ||
                configured.Username != selected.Username || receipt.Schema != admission.Schema)
            {
                throw new InvalidOperationException("socket_receipt_database_scope_conflict");
            }
            // Both the actual connection AND transaction are shared; this context owns neither.
            // https://learn.microsoft.com/en-us/ef/core/saving/transactions#cross-context-transaction
            receipt.Database.SetDbConnection(admissionConnection, contextOwnsConnection: false);
            await receipt.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);
            return receipt;
        }
        catch
        {
            await receipt.DisposeAsync();
            throw;
        }
    }

    internal static void DemandProvider(DbContext context)
    {
        if (context.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL" || context.Database.CreateExecutionStrategy().RetriesOnFailure)
        {
            throw new InvalidOperationException("socket_receipt_selected_provider_required");
        }
    }
}
