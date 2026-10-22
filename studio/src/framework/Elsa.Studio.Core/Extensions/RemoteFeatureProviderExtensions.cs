using System.Net;
using Elsa.Studio.Contracts;
using Refit;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Contains extension methods for remote feature checks.
/// </summary>
public static class RemoteFeatureProviderExtensions
{
    /// <summary>
    /// Checks whether the specified remote feature is enabled, returning <c>false</c> when the check cannot complete.
    /// </summary>
    public static async Task<bool> IsEnabledOrDefaultAsync(this IRemoteFeatureProvider remoteFeatureProvider, string featureName, CancellationToken cancellationToken = default)
        => await IsEnabledOrDefaultAsync(() => remoteFeatureProvider.IsEnabledAsync(featureName, cancellationToken));

    /// <summary>
    /// Checks the current remote feature catalog without reusing a cached result, returning <c>false</c> when the check cannot complete.
    /// </summary>
    public static async Task<bool> IsEnabledFreshOrDefaultAsync(this IRemoteFeatureProviderWithFreshCheck remoteFeatureProvider, string featureName, CancellationToken cancellationToken = default)
        => await IsEnabledOrDefaultAsync(() => remoteFeatureProvider.IsEnabledFreshAsync(featureName, cancellationToken));

    private static async Task<bool> IsEnabledOrDefaultAsync(Func<Task<bool>> check)
    {
        try
        {
            return await check();
        }
        catch (ApiException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return false;
        }
        catch (ApiException)
        {
            return false;
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (InvalidOperationException e) when (IsJavaScriptInteropUnavailable(e))
        {
            return false;
        }
    }

    private static bool IsJavaScriptInteropUnavailable(InvalidOperationException e) =>
        e.Message.Contains("JavaScript interop calls cannot be issued at this time", StringComparison.Ordinal);
}
