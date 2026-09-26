using System.DirectoryServices.Protocols;

namespace Elsa.Ldap.Extensions;

/// <summary>
/// Async extension methods for <see cref="LdapConnection"/> built on top of
/// the existing APM BeginSendRequest / EndSendRequest / Abort surface.
/// </summary>
internal static class LdapConnectionExtensions
{
    /// <summary>
    /// Sends an LDAP request asynchronously and returns the response cast to <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">
    /// The expected <see cref="DirectoryResponse"/> subtype.
    /// Prefer the concrete overloads (e.g. pass an <see cref="AddRequest"/> to get back
    /// a <see cref="Task{AddResponse}"/>) to avoid specifying this  type argument explicitly.
    /// </typeparam>
    /// <param name="connection">The LDAP connection to use.</param>
    /// <param name="request">The directory request to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with the strongly-typed response.</returns>
    /// <exception cref="InvalidCastException">Thrown when the actual response type does not match <typeparamref name="TResponse"/>.</exception>
    public static async Task<TResponse> SendRequestAsync<TResponse>(
        this LdapConnection connection,
        DirectoryRequest request,
        CancellationToken cancellationToken = default)
        where TResponse : DirectoryResponse
    {
        var response = await SendRequestCoreAsync(connection, request, cancellationToken)
            .ConfigureAwait(false);

        if (response is not TResponse typed)
        {
            throw new InvalidCastException($"Expected a response of type {typeof(TResponse).Name} but received {response?.GetType().Name ?? "null"}.");
        }

        return typed;
    }

    private static Task<DirectoryResponse> SendRequestCoreAsync(
        LdapConnection connection,
        DirectoryRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IAsyncResult? asyncResult = null;
        CancellationTokenRegistration ctr = default;

        var tcs = new TaskCompletionSource<DirectoryResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        AsyncCallback callback = ar =>
        {
            // Unregister the cancellation hook first so we don't race.
            ctr.Dispose();

            // CancellationToken fired before the callback ran — skip EndSendRequest.
            if (tcs.Task.IsCanceled)
            {
                return;
            }

            try
            {
                tcs.TrySetResult(connection.EndSendRequest(ar));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        };

        try
        {
            asyncResult = connection.BeginSendRequest(
                request: request,
                partialMode: PartialResultProcessing.NoPartialResultSupport,
                callback: callback,
                state: null);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
            return tcs.Task;
        }

        // Register cancellation after BeginSendRequest so we always have a valid IAsyncResult to pass to Abort().
        if (cancellationToken.CanBeCanceled)
        {
            ctr = cancellationToken.Register(
                callback: state =>
                {
                    var (conn, ar, t) = ((LdapConnection, IAsyncResult, TaskCompletionSource<DirectoryResponse>))state!;

                    // Signal cancellation first so the callback skips EndSendRequest.
                    t.TrySetCanceled();
                    try { conn.Abort(ar); }
                    catch { /* Abort is best-effort */ }
                },
                state: (connection, asyncResult, tcs),
                useSynchronizationContext: false);

            // Handle the race where the token was cancelled between BeginSendRequest returning and the Register call above.
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                try { connection.Abort(asyncResult); }
                catch { /* best-effort */ }
            }
        }

        return tcs.Task;
    }
}