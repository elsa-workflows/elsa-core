using Elsa.Telnyx.Client.Models;
using Refit;

namespace Elsa.Telnyx.Extensions;

/// <summary>
/// Provides extensions for <see cref="ApiException"/>.
/// </summary>
public static class ApiExceptionExtensions
{
    /// <summary>
    /// Reads a <see cref="ErrorResponse"/> from the specified <see cref="ApiException"/>.
    /// </summary>
    public static async Task<ErrorResponse> GetErrorResponseAsync(this ApiException e, CancellationToken cancellationToken = default)
    {
        var httpContent = new StringContent(e.Content!);
        return (await e.RefitSettings.ContentSerializer.FromHttpContentAsync<ErrorResponse>(httpContent, cancellationToken))!;
    }

    /// <summary>
    /// Returns <code>true</code> if the specified exception represents a failure due to the call no longer being active.
    /// </summary>
    public static async Task<bool> CallIsNoLongerActiveAsync(this ApiException e, CancellationToken cancellationToken = default)
    {
        var errorResponse = await e.GetErrorResponseAsync(cancellationToken);
        var errors = errorResponse.Errors;
        return errors.Any(x => x.Code == ErrorCodes.CallHasAlreadyEnded);
    }
}