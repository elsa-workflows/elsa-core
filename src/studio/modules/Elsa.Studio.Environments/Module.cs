using System.Net;
using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Components;
using Elsa.Studio.Environments.Tasks;
using Elsa.Studio.Models;
using Microsoft.Extensions.Logging;
using Refit;

namespace Elsa.Studio.Environments;

/// <summary>
/// Represents the environments feature module for managing server environment selection in the app bar.
/// </summary>
public class Feature(
    IAppBarService appBarService,
    LoadEnvironmentsStartupTask loadEnvironmentsStartupTask,
    ILogger<Feature> logger) : FeatureBase
{
    /// <inheritdoc />
    public override async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Circuit-scoped load so Blazor Server pickers see the same IEnvironmentService instance.
            await loadEnvironmentsStartupTask.LoadAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Could not reach the environments API; the picker will stay empty.");
        }
        catch (ApiException exception) when (exception.StatusCode is HttpStatusCode.NotFound
            or HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden)
        {
            logger.LogWarning("Environments API returned {StatusCode}; the picker will stay empty.", (int)exception.StatusCode);
        }

        appBarService.AddElement(new AppBarElement<EnvironmentPicker>());
    }
}
