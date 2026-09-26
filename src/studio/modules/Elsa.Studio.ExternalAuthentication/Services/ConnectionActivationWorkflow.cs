using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;

namespace Elsa.Studio.ExternalAuthentication.Services;

internal enum ConnectionActivationStatus
{
    Succeeded,
    ValidationFailed,
    RevisionChanged,
    EnableFailed,
    CompletionUnknown
}

internal sealed record ConnectionActivationResult(
    ConnectionActivationStatus Status,
    ConnectionDetail Connection,
    ConnectionValidationResult? Validation = null,
    Exception? Error = null);

internal static class ConnectionActivationWorkflow
{
    public static async Task<ConnectionActivationResult> ExecuteAsync(
        IExternalAuthenticationConnectionsApi api,
        ConnectionDetail candidate,
        bool requiresValidation,
        Func<string, Task> reportProgress,
        CancellationToken cancellationToken = default)
    {
        ConnectionValidationResult? validation = null;
        var authoritative = candidate;

        if (requiresValidation)
        {
            await reportProgress("Checking configuration…");
            validation = await api.ValidateAsync(candidate.Id, cancellationToken);
            authoritative = await api.GetAsync(candidate.Id, cancellationToken);
            if (authoritative.Revision != candidate.Revision)
                return new ConnectionActivationResult(ConnectionActivationStatus.RevisionChanged, authoritative, validation);

            if (!string.Equals(authoritative.Validity, "valid", StringComparison.OrdinalIgnoreCase))
                return new ConnectionActivationResult(ConnectionActivationStatus.ValidationFailed, authoritative, validation);
        }

        await reportProgress("Making available for sign-in…");
        try
        {
            await api.EnableAsync(authoritative.Id, ConnectionConcurrency.ToIfMatch(authoritative.Revision), cancellationToken);
        }
        catch (Exception exception)
        {
            try
            {
                var reconciled = await api.GetAsync(authoritative.Id, cancellationToken);
                return IsConfirmedAvailable(reconciled)
                    ? new ConnectionActivationResult(ConnectionActivationStatus.Succeeded, reconciled, validation)
                    : new ConnectionActivationResult(ConnectionActivationStatus.EnableFailed, reconciled, validation, exception);
            }
            catch
            {
                return new ConnectionActivationResult(ConnectionActivationStatus.CompletionUnknown, authoritative, validation, exception);
            }
        }

        try
        {
            var enabled = await api.GetAsync(authoritative.Id, cancellationToken);
            return IsConfirmedAvailable(enabled)
                ? new ConnectionActivationResult(ConnectionActivationStatus.Succeeded, enabled, validation)
                : new ConnectionActivationResult(ConnectionActivationStatus.EnableFailed, enabled, validation);
        }
        catch (Exception exception)
        {
            return new ConnectionActivationResult(ConnectionActivationStatus.CompletionUnknown, authoritative, validation, exception);
        }
    }

    private static bool IsConfirmedAvailable(ConnectionDetail connection) =>
        connection.EnabledIntent && connection.EffectivelyEnabled;
}
