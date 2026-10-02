using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Microsoft.Extensions.Logging;
using Refit;

namespace Elsa.Studio.Environments.Tasks;

/// <summary>
/// Loads available environments from the current backend into <see cref="IEnvironmentService"/>.
/// </summary>
public class LoadEnvironmentsStartupTask(
    IBackendApiClientProvider backendApiClientProvider,
    IEnvironmentService environmentService,
    ILogger<LoadEnvironmentsStartupTask> logger) : IStartupTask
{
    /// <summary>
    /// Fetches environments through <see cref="IBackendApiClientProvider"/> and stores them.
    /// </summary>
    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        var environmentsClient = await backendApiClientProvider.GetApiAsync<IEnvironmentsClient>(cancellationToken);
        var response = await environmentsClient.ListEnvironmentsAsync(cancellationToken);
        environmentService.SetEnvironments(response.Environments, response.DefaultEnvironmentName);
    }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await LoadAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            // Hosted-service startup can run before the circuit has a backend connection.
            logger.LogWarning(exception, "Could not reach the environments API during startup.");
        }
        catch (ApiException exception) when (exception.StatusCode is HttpStatusCode.NotFound
            or HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden)
        {
            logger.LogWarning("Environments API returned {StatusCode} during startup; environments will load after login if the API is available.", (int)exception.StatusCode);
        }
    }
}
