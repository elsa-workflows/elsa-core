using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Slack.Services;
using Elsa.Slack.SocketMode.Credentials;
using Elsa.Slack.SocketMode.Events;
using Elsa.Slack.SocketMode.Persistence;
using Elsa.Slack.SocketMode.Transport;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.Slack.SocketMode.Extensions;

/// <summary>Explicit opt-in shared by classic and Shell hosts. Existing guarded workflow, Connections and Secrets persistence is selected separately.</summary>
public static class SlackSocketModeServiceCollectionExtensions
{
    /// <summary>Registers the fixed production Socket listener. The listener validates existing provisioning before opening; it never migrates or activates.</summary>
    public static IServiceCollection AddSlackSocketMode(this IServiceCollection services, SlackSocketModeConfiguration configuration, string connectionString) =>
        services.AddSlackSocketMode(configuration, connectionString, SlackSocketTransportPolicy.Production);

    // Only the audited friend fixtures can select the separately validated literal-loopback transport.
    internal static IServiceCollection AddSlackSocketMode(this IServiceCollection services, SlackSocketModeConfiguration configuration,
        string connectionString, SlackSocketTransportPolicy transportPolicy)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(transportPolicy);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("An explicit PostgreSQL connection string is required for Socket receipts.", nameof(connectionString));
        }
        var ownedContracts = new[]
        {
            typeof(SlackSocketModeConfiguration), typeof(SlackSocketTransportPolicy), typeof(ISlackPublicChannelMessageSource),
            typeof(AdmittedSlackPublicChannelMessageSource), typeof(SlackSocketListenerCredentialReader), typeof(SlackSocketEnvelopeProcessor),
            typeof(SlackSocketUrlOpener), typeof(SlackSocketModeHostValidator), typeof(SlackSocketSubscriptionWithdrawal),
            typeof(SlackSocketModeHealth), typeof(ISlackSocketModeHealth), typeof(SlackSocketListener), typeof(SlackSocketReceiptElsaDbContext),
            typeof(DbContextOptions<SlackSocketReceiptElsaDbContext>), typeof(IDbContextFactory<SlackSocketReceiptElsaDbContext>),
            typeof(SlackSocketReceiptTransactions), typeof(ISlackSocketDiscardStore), typeof(IAdmissionIdentityConflictReader)
        };
        if (services.Any(descriptor => ownedContracts.Contains(descriptor.ServiceType)))
        {
            throw new InvalidOperationException("Socket Mode requires one explicit registration with its reviewed services and receipt store.");
        }
        services.AddSingleton(configuration);
        services.AddSingleton(transportPolicy);
        services.TryAddSingleton(TimeProvider.System);
        // Legacy Slack has no Shell feature. Keep the same client-factory baseline as its classic SlackFeature.
        services.TryAddSingleton<SlackClientFactory>();
        services.AddScoped<ISlackPublicChannelMessageSource, AdmittedSlackPublicChannelMessageSource>();
        services.AddScoped<SlackSocketListenerCredentialReader>();
        services.AddScoped<SlackSocketEnvelopeProcessor>();
        services.AddScoped<SlackSocketUrlOpener>();
        services.AddScoped<SlackSocketModeHostValidator>();
        services.AddScoped<SlackSocketSubscriptionWithdrawal>();
        services.AddSingleton(_ => new SlackSocketModeHealth(configuration.Limits.MaximumPendingEnvelopes,
            configuration.Limits.MaximumAdmissionConcurrency));
        services.AddSingleton<ISlackSocketModeHealth>(provider => provider.GetRequiredService<SlackSocketModeHealth>());
        services.AddDbContextFactory<SlackSocketReceiptElsaDbContext>((_, builder) =>
            builder.UseElsaPostgreSql(typeof(SlackSocketReceiptElsaDbContext).Assembly, connectionString,
                new ElsaDbContextOptions { SchemaName = "Elsa", MigrationsHistoryTableName = SlackSocketReceiptElsaDbContext.HistoryTable }));
        services.AddSlackSocketDiscardPersistence();
        services.AddHostedService<SlackSocketListener>();
        return services;
    }

}
