using System.Net;
using Elsa.Api.Client.Resources.Identity.Contracts;
using Elsa.Api.Client.Resources.Identity.Requests;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.Testing;
using Refit;
using Xunit;
using ElsaIdentityValidator = Elsa.Studio.Authentication.ElsaIdentity.Services.ElsaIdentityCredentialsValidator;
using LegacyValidator = Elsa.Studio.Login.Services.ElsaIdentityCredentialsValidator;

namespace Elsa.Studio.ExternalAuthentication.Tests.Login;

/// <summary>
/// Verifies that the Elsa Identity credentials validators translate the login endpoint's HTTP status codes:
/// 401 means invalid credentials, 200 returns tokens, and any other error propagates.
/// </summary>
public sealed class ElsaIdentityCredentialsValidatorTests
{
    [Fact]
    public async Task LoginReturning401IsValidatedAsInvalidCredentialsByLegacyValidator()
    {
        var api = new FakeLoginApi(exception: ApiExceptions.Create(HttpStatusCode.Unauthorized));
        var validator = new LegacyValidator(new FakeAnonymousBackendApiClientProvider(api));

        var result = await validator.ValidateCredentialsAsync("admin", "wrong-password");

        Assert.False(result.IsAuthenticated);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
    }

    [Fact]
    public async Task SuccessfulLoginReturnsTokensFromLegacyValidator()
    {
        var api = new FakeLoginApi(response: new LoginResponse(true, "access-a", "refresh-a"));
        var validator = new LegacyValidator(new FakeAnonymousBackendApiClientProvider(api));

        var result = await validator.ValidateCredentialsAsync("admin", "password");

        Assert.True(result.IsAuthenticated);
        Assert.Equal("access-a", result.AccessToken);
        Assert.Equal("refresh-a", result.RefreshToken);
    }

    [Fact]
    public async Task LoginErrorOtherThan401PropagatesFromLegacyValidator()
    {
        var api = new FakeLoginApi(exception: ApiExceptions.Create(HttpStatusCode.InternalServerError));
        var validator = new LegacyValidator(new FakeAnonymousBackendApiClientProvider(api));

        await Assert.ThrowsAsync<ApiException>(() => validator.ValidateCredentialsAsync("admin", "password").AsTask());
    }

    [Fact]
    public async Task LoginReturning401IsValidatedAsInvalidCredentialsByElsaIdentityValidator()
    {
        var api = new FakeLoginApi(exception: ApiExceptions.Create(HttpStatusCode.Unauthorized));
        var validator = new ElsaIdentityValidator(new FakeAnonymousBackendApiClientProvider(api));

        var result = await validator.ValidateCredentialsAsync("admin", "wrong-password");

        Assert.False(result.IsValid);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
    }

    [Fact]
    public async Task SuccessfulLoginReturnsTokensFromElsaIdentityValidator()
    {
        var api = new FakeLoginApi(response: new LoginResponse(true, "access-a", "refresh-a"));
        var validator = new ElsaIdentityValidator(new FakeAnonymousBackendApiClientProvider(api));

        var result = await validator.ValidateCredentialsAsync("admin", "password");

        Assert.True(result.IsValid);
        Assert.Equal("access-a", result.AccessToken);
        Assert.Equal("refresh-a", result.RefreshToken);
    }

    [Fact]
    public async Task LoginErrorOtherThan401PropagatesFromElsaIdentityValidator()
    {
        var api = new FakeLoginApi(exception: ApiExceptions.Create(HttpStatusCode.InternalServerError));
        var validator = new ElsaIdentityValidator(new FakeAnonymousBackendApiClientProvider(api));

        await Assert.ThrowsAsync<ApiException>(() => validator.ValidateCredentialsAsync("admin", "password").AsTask());
    }

    private sealed class FakeAnonymousBackendApiClientProvider(ILoginApi loginApi) : IAnonymousBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/elsa/api/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromResult((T)(object)loginApi);
    }

    private sealed class FakeLoginApi(Exception? exception = null, LoginResponse? response = null) : ILoginApi
    {
        public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            exception is not null
                ? Task.FromException<LoginResponse>(exception)
                : Task.FromResult(response ?? new LoginResponse(false, null, null));
    }
}
