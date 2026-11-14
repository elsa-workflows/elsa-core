using System.Net;
using Elsa.Api.Client.Resources.Identity.Contracts;
using Elsa.Api.Client.Resources.Identity.Requests;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Models;
using Elsa.Studio.Contracts;
using Refit;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <summary>
/// An implementation of <see cref="ICredentialsValidator"/> that consumes endpoints from Elsa.Identity.
/// </summary>
public class ElsaIdentityCredentialsValidator(IAnonymousBackendApiClientProvider backendApiClientProvider) : ICredentialsValidator
{
    /// <inheritdoc />
    public async ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<ILoginApi>(cancellationToken);
        var request = new LoginRequest(username, password);

        try
        {
            var response = await api.LoginAsync(request, cancellationToken);
            return new(response.IsAuthenticated, response.AccessToken, response.RefreshToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // The login endpoint rejects invalid credentials with HTTP 401; that is not a session to refresh, just a failed sign-in.
            return new(false, null, null);
        }
    }
}
