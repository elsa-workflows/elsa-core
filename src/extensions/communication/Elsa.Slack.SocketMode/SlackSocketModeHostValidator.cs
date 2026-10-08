using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Persistence.EFCore;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Repositories;
using Elsa.Secrets.Services;
using Elsa.Secrets.Stores;
using Elsa.Slack.SocketMode.Credentials;
using Elsa.Slack.SocketMode.Events;
using Elsa.Slack.SocketMode.Persistence;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Slack.SocketMode;

// Called before bootstrap/activation or opening the socket. This extends the SAME explicit
// immutable Admission audit inventory and never enables a public credential or execution lane.
internal sealed class SlackSocketModeHostValidator(SlackSocketModeConfiguration configuration,
    AdmissionHostConfiguration admission, IServiceProvider services)
{
    internal async ValueTask ValidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await admission.ValidateAsync(services, cancellationToken);
            if (admission.TenantId != configuration.TenantId || admission.EnvironmentId != configuration.EnvironmentId ||
                services.GetRequiredService<AdmissionPersistenceScope>() != new AdmissionPersistenceScope(configuration.TenantId, configuration.EnvironmentId) ||
                services.GetRequiredService<ISlackPublicChannelMessageSource>().GetType() != typeof(AdmittedSlackPublicChannelMessageSource) ||
                services.GetRequiredService<SlackSocketListenerCredentialReader>().GetType() != typeof(SlackSocketListenerCredentialReader) ||
                services.GetRequiredService<IConnectionLifecycleStore>().GetType() != typeof(EFCoreConnectionLifecycleStore) ||
                services.GetRequiredService<IAdmissionStore>().GetType() != typeof(EFCoreAdmissionStore) ||
                services.GetRequiredService<IManagedSecretManager>().GetType() != typeof(DefaultSecretManager) ||
                services.GetRequiredService<ISecretManager>().GetType() != typeof(DefaultSecretManager) ||
                services.GetRequiredService<ISecretRepository>().GetType() != typeof(EFCoreSecretRepository) ||
                services.GetRequiredService<ISecretStoreRegistry>().GetType() != typeof(SecretStoreRegistry) ||
                services.GetRequiredService<ISecretNameValidator>().GetType() != typeof(DefaultSecretNameValidator) ||
                services.GetRequiredService<ISecretValueProtector>().GetType() != typeof(DefaultSecretValueProtector) ||
                services.GetRequiredService<ISecretStoreRegistry>().Get(SecretStoreNames.Encrypted).GetType() != typeof(EncryptedSecretStore))
            {
                throw new InvalidOperationException();
            }
            foreach (var subscription in configuration.Subscriptions)
            {
                admission.ValidateSubscription(subscription.Configuration);
            }
            foreach (var service in new object[]
            {
                services.GetRequiredService<IConnectionUseAuthorizer>(), services.GetRequiredService<ILoggerFactory>(),
                services.GetRequiredService<ITenantAccessor>()
            })
            {
                admission.DemandAuditedServiceType(service.GetType());
            }
            foreach (var provider in services.GetServices<ILoggerProvider>())
            {
                admission.DemandAuditedServiceType(provider.GetType());
            }
            foreach (var store in services.GetRequiredService<ISecretStoreRegistry>().List())
            {
                if (store.GetType() != typeof(EncryptedSecretStore))
                {
                    admission.DemandAuditedServiceType(store.GetType());
                }
            }
            foreach (var handler in services.GetServices<IEntityModelCreatingHandler>().Cast<object>().Concat(services.GetServices<IEntitySavingHandler>()))
            {
                admission.DemandAuditedServiceType(handler.GetType());
            }
            using var tenant = services.GetRequiredService<ITenantAccessor>().PushContext(new Tenant { Id = configuration.TenantId, Name = configuration.TenantId });
            var ledger = await ValidateDatabaseAsync<AdmissionElsaDbContext>(cancellationToken);
            var connections = await ValidateDatabaseAsync<ConnectionsElsaDbContext>(cancellationToken);
            var secrets = await ValidateDatabaseAsync<SecretsElsaDbContext>(cancellationToken);
            if (ledger != connections || ledger != secrets)
            {
                throw new InvalidOperationException();
            }
            await services.GetRequiredService<SlackSocketListenerCredentialReader>().DemandAuthorizedAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Socket host validation was cancelled.", cancellationToken);
        }
        catch (Exception)
        {
            // No inner provider exception, SQL, connection string or secret leaves startup.
            throw new InvalidOperationException("The Socket listener requires the reviewed isolated PostgreSQL host and explicit listener authority.");
        }
    }

    private async Task<(string DataSource, string Database)> ValidateDatabaseAsync<TContext>(CancellationToken cancellationToken)
        where TContext : ElsaDbContextBase
    {
        var factory = services.GetRequiredService<IDbContextFactory<TContext>>();
        admission.DemandAuditedServiceType(factory.GetType());
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        DemandDatabaseLayout(context, ElsaDbContextBase.MigrationsHistoryTable);
        if (context.GetType() != typeof(TContext) || context.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL" ||
            context.Schema != "Elsa" || context.Model.GetDefaultSchema() != "Elsa" ||
            !context.IsTenantFilteringEnabled || context.TenantId != configuration.TenantId ||
            context.Database.CreateExecutionStrategy().RetriesOnFailure ||
            context.Database.HasPendingModelChanges() ||
            !context.Database.GetMigrations().Any() || (await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            throw new InvalidOperationException();
        }
        var connection = context.Database.GetDbConnection();
        // Compared only in memory. These identifiers are never included in a diagnostic or receipt.
        return (connection.DataSource, connection.Database);
    }

    internal static void DemandDatabaseLayout(ElsaDbContextBase context, string sharedHistory)
    {
        // Defaults selected by PostgreSqlAdmissionPersistenceExtensions/ShellFeature and
        // ElsaDbContextBase through UseElsaPostgreSql. Connections and Secrets intentionally
        // share the existing history; the Admission ledger must remain separate from it.
        const string admissionHistory = "__AdmissionMigrationsHistory";
        var expectedHistory = context.GetType() == typeof(AdmissionElsaDbContext) ? admissionHistory :
            context.GetType() == typeof(ConnectionsElsaDbContext) || context.GetType() == typeof(SecretsElsaDbContext)
                ? sharedHistory : throw new InvalidOperationException();
        var options = RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>());
        if (string.IsNullOrWhiteSpace(sharedHistory) || sharedHistory == admissionHistory ||
            sharedHistory == SlackSocketReceiptElsaDbContext.HistoryTable ||
            options.MigrationsHistoryTableName != expectedHistory || options.MigrationsHistoryTableSchema != "Elsa" ||
            context.Schema != "Elsa" || context.Model.GetDefaultSchema() != "Elsa" ||
            context.Model.GetEntityTypes().Any(x => x.GetSchema() != "Elsa"))
        {
            throw new InvalidOperationException("Socket credential and event stores require the reviewed migration histories and table schemas.");
        }
    }
}
