using Elsa.Workflows.Admission.Persistence.EFCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Slack.SocketMode.Persistence;

internal static class SlackSocketDiscardPersistenceExtensions
{
    // Root feature configures the actual context factory, explicit trusted scope and provider.
    // It must also attest exactly these registrations before listener activation.
    internal static IServiceCollection AddSlackSocketDiscardPersistence(this IServiceCollection services)
    {
        services.AddScoped<SlackSocketReceiptTransactions>();
        services.AddScoped<IAdmissionIdentityConflictReader, PostgreSqlSlackSocketIdentityConflictReader>();
        services.AddScoped<ISlackSocketDiscardStore, PostgreSqlSlackSocketDiscardStore>();
        return services;
    }
}
