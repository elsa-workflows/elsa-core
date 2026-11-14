using System.Net;
using Elsa.Api.Client.Resources.Identity.Contracts;
using Elsa.Api.Client.Resources.Identity.Requests;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Refit;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// A implementation of <see cref="ICredentialsValidator"/> that consumes the endpoints from Elsa.Identity.
/// </summary>
public class ElsaIdentityCredentialsValidator(IAnonymousBackendApiClientProvider backendApiClientProvider) : ICredentialsValidator
{
    /// <inheritdoc />
    public async ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<ILoginApi>(cancellationToken);

        var request = new LoginRequest(username, password);

        LoginResponse response;
        try
        {
            response = await api.LoginAsync(request, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // The login endpoint rejects invalid credentials with HTTP 401; that is not a session to refresh, just a failed sign-in.
            return new(false, null, null);
        }

        return new(response.IsAuthenticated, response.AccessToken, response.RefreshToken);
    }
}
