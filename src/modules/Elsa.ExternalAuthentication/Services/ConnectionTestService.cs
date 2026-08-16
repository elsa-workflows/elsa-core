using System.Diagnostics;
using System.Security.Claims;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>Runs an explicit adapter test and stores only its latest redacted outcome.</summary>
public sealed class ConnectionTestService(
    IdentityProviderConnectionManagementService management,
    IExternalAuthenticationAdapterRegistry adapters,
    IEnumerable<ISecretBindingResolver> secretBindingResolvers,
    IConnectionObservationStore observations,
    ISystemClock clock,
    ExternalAuthenticationSecurityNotifier notifier)
{
    private readonly IReadOnlyDictionary<string, ISecretBindingResolver> _resolvers = secretBindingResolvers.ToDictionary(x => x.Type, StringComparer.Ordinal);

    public async ValueTask<ConnectionTestOperationResult> TestAsync(string connectionId, long expectedRevision, string tenantId, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var lookup = await management.FindAsync(connectionId, tenantId, cancellationToken);
        if (lookup is not ManagementConnectionLookupResult.Found(var connection))
            return new ConnectionTestOperationResult.NotFound();
        if (connection.Connection.Revision != expectedRevision)
            return new ConnectionTestOperationResult.PreconditionFailed(connection.Connection.Revision);
        if (!adapters.TryGet(connection.Connection.AdapterType, out var adapter))
            return new ConnectionTestOperationResult.Unavailable();

        var timer = Stopwatch.StartNew();
        ConnectionTestResult test;
        IReadOnlyDictionary<string, ResolvedSecretBinding> secrets = new Dictionary<string, ResolvedSecretBinding>();
        try
        {
            secrets = await ResolveSecretsAsync(connection.Connection.SecretBindings, cancellationToken);
            test = await adapter.TestAsync(new ConnectionTestContext(connection, secrets, clock), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            test = new ConnectionTestResult(ConnectionObservationStatus.Failed, "unavailable", "The provider test could not be completed.", []);
        }
        finally
        {
            timer.Stop();
            foreach (var secret in secrets.Values)
                secret.Value.Dispose();
        }

        var observation = new ConnectionObservation(
            connection.Connection.Id,
            connection.Connection.MaterialRevision,
            clock.UtcNow,
            test.Status,
            SafeCategory(test.Category),
            timer.Elapsed,
            SafeSummary(test.Summary),
            test.Warnings.Select(SafeSummary).ToArray(),
            Guid.NewGuid().ToString("N"));
        await observations.SaveLatestAsync(observation, cancellationToken);
        await notifier.PublishAsync(new IdentityProviderConnectionTested(
            ExternalAuthenticationSecurityNotifier.Context(
                ActorId(actor),
                tenantId,
                connection.Connection.Id,
                null,
                observation.Status == ConnectionObservationStatus.Failed ? SecurityEventOutcome.Failed : SecurityEventOutcome.Succeeded,
                "Identity provider connection test completed."),
            observation.TestedMaterialRevision,
            observation.Status.ToString().ToLowerInvariant(),
            observation.Category,
            observation.Duration), cancellationToken);
        return new ConnectionTestOperationResult.Completed(observation);
    }

    private async ValueTask<IReadOnlyDictionary<string, ResolvedSecretBinding>> ResolveSecretsAsync(IDictionary<string, SecretBinding> bindings, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, ResolvedSecretBinding>(StringComparer.Ordinal);
        try
        {
            foreach (var (name, binding) in bindings)
            {
                if (!_resolvers.TryGetValue(binding.ResolverType, out var resolver))
                    throw new InvalidOperationException("The secret binding resolver is unavailable.");
                result[name] = await resolver.ResolveAsync(binding, cancellationToken);
            }
            return result;
        }
        catch
        {
            foreach (var secret in result.Values)
                secret.Value.Dispose();
            throw;
        }
    }

    private static string? ActorId(ClaimsPrincipal actor) => actor.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? actor.FindFirst("sub")?.Value;
    // Adapter messages are already contractually safe, but cap them at a predictable diagnostic size.
    private static string SafeSummary(string value) => string.IsNullOrWhiteSpace(value) ? "No additional details are available." : value.Length <= 512 ? value : value[..512];
    private static string SafeCategory(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Length <= 128 ? value : "unknown";
}

public abstract record ConnectionTestOperationResult
{
    private ConnectionTestOperationResult() { }
    public sealed record Completed(ConnectionObservation Observation) : ConnectionTestOperationResult;
    public sealed record NotFound : ConnectionTestOperationResult;
    public sealed record PreconditionFailed(long CurrentRevision) : ConnectionTestOperationResult;
    public sealed record Unavailable : ConnectionTestOperationResult;
}
