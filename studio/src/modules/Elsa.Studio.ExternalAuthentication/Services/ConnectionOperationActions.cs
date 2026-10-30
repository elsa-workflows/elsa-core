using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Refit;

namespace Elsa.Studio.ExternalAuthentication.Services;

public static class ConnectionOperationActions
{
    public const string TestCompletedMessage = "Connection test completed. The result contains only redacted diagnostics.";
    public const string TestFailedMessage = "The connection test failed before a diagnostic observation was recorded. Check the connection requirements and server logs, then try again.";

    public static async Task<ConnectionObservation> TestConnectionAsync(
        IBackendApiClientProvider apiClientProvider,
        ConnectionSummary connection,
        CancellationToken cancellationToken = default)
    {
        var api = await apiClientProvider.GetApiAsync<IExternalAuthenticationOperationsApi>(cancellationToken);
        var result = await api.TestAsync(
            connection.Id,
            ConnectionConcurrency.ToIfMatch(connection.Revision),
            cancellationToken);
        return result.ToObservation();
    }

    public static string PresentError(Exception exception, string fallbackMessage) =>
        exception is ApiException apiException
            ? ConnectionManagementError
                .Parse(apiException.StatusCode, apiException.Content, fallbackMessage)
                .OperationalDisplayMessage
            : fallbackMessage;
}
