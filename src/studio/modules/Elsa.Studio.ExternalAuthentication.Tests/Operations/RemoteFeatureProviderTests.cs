using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Refit;
using System.Net;
using System.Security.Claims;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Operations;

public class RemoteFeatureProviderTests
{
    [Fact]
    public async Task FeatureChecks_ReuseTheInstalledFeatureCatalog()
    {
        var api = new FeaturesApi();
        var provider = new RemoteFeatureProvider(new BackendApiClientProvider(api));

        Assert.True(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.False(await provider.IsEnabledAsync("Elsa.AI"));
        Assert.Equal(1, api.ListCalls);
        Assert.Equal(0, api.GetCalls);
    }

    [Fact]
    public async Task AnonymousFeatureChecks_DoNotProbeTheProtectedBackend()
    {
        var api = new FeaturesApi();
        var authentication = new AuthenticationStateProviderStub();
        var provider = new RemoteFeatureProvider(new BackendApiClientProvider(api), authentication);

        Assert.False(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.Equal(0, api.ListCalls);

        authentication.IsAuthenticated = true;
        Assert.True(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.Equal(1, api.ListCalls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task AuthenticationFailures_AreRetried(HttpStatusCode statusCode)
    {
        var api = new FeaturesApi();
        api.Responses.Enqueue(_ => Task.FromException<ListResponse<FeatureDescriptor>>(CreateApiException(statusCode)));
        var provider = new RemoteFeatureProvider(new BackendApiClientProvider(api));

        Assert.False(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.True(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.Equal(2, api.ListCalls);
    }

    [Fact]
    public async Task MissingCatalog_IsCached()
    {
        var api = new FeaturesApi();
        api.Responses.Enqueue(_ => Task.FromException<ListResponse<FeatureDescriptor>>(CreateApiException(HttpStatusCode.NotFound)));
        var provider = new RemoteFeatureProvider(new BackendApiClientProvider(api));

        Assert.False(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.False(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.Equal(1, api.ListCalls);
    }

    [Fact]
    public async Task CancelledCatalogRequest_IsRetried()
    {
        var api = new FeaturesApi();
        api.Responses.Enqueue(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("The cancellation token should stop this request.");
        });
        var provider = new RemoteFeatureProvider(new BackendApiClientProvider(api));
        using var cancellationTokenSource = new CancellationTokenSource();

        var cancelledCheck = provider.IsEnabledAsync("Elsa.ExternalAuthentication", cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledCheck);
        Assert.True(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.Equal(2, api.ListCalls);
    }

    [Fact]
    public async Task ConcurrentFeatureChecks_ShareOneCatalogRequest()
    {
        var responseReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new FeaturesApi();
        api.Responses.Enqueue(async _ =>
        {
            await responseReady.Task;
            return FeaturesApi.InstalledFeatures;
        });
        var provider = new RemoteFeatureProvider(new BackendApiClientProvider(api));

        var firstCheck = provider.IsEnabledAsync("Elsa.ExternalAuthentication");
        var secondCheck = provider.IsEnabledAsync("Elsa.ExternalAuthentication");

        Assert.Equal(1, api.ListCalls);
        responseReady.SetResult();
        Assert.All(await Task.WhenAll(firstCheck, secondCheck), Assert.True);
        Assert.Equal(1, api.ListCalls);
    }

    private static ApiException CreateApiException(HttpStatusCode statusCode)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://elsa.example.test/features");
        using var response = new HttpResponseMessage(statusCode) { RequestMessage = request };
        return ApiException.Create(request, HttpMethod.Get, response, new RefitSettings()).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task FeatureChecks_RefetchWhenBackendUrlChanges()
    {
        var api = new FeaturesApi();
        var backend = new BackendApiClientProvider(api);
        var provider = new RemoteFeatureProvider(backend);

        Assert.True(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.Equal(1, api.ListCalls);

        backend.Url = new("https://other.example.test/");
        Assert.True(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.Equal(2, api.ListCalls);
    }

    [Fact]
    public async Task FeatureChecks_DoNotCacheUnderStaleUrlWhenEnvironmentChangesMidLoad()
    {
        var firstListStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstList = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new FeaturesApi();
        api.Responses.Enqueue(async _ =>
        {
            firstListStarted.SetResult();
            await releaseFirstList.Task;
            return FeaturesFor("Elsa.A");
        });
        api.Responses.Enqueue(_ => Task.FromResult(FeaturesFor("Elsa.B")));
        api.Responses.Enqueue(_ => Task.FromResult(FeaturesFor("Elsa.A")));

        var backend = new BackendApiClientProvider(api);
        var provider = new RemoteFeatureProvider(backend);

        var firstCheck = provider.IsEnabledAsync("Elsa.A");
        await firstListStarted.Task;

        // A second check waits on the catalog lock, then must re-read Url after GetApiAsync.
        var secondCheck = provider.IsEnabledAsync("Elsa.A");
        backend.Url = new("https://b.example.test/");
        releaseFirstList.SetResult();

        Assert.True(await firstCheck);
        Assert.False(await secondCheck);

        backend.Url = new("https://elsa.example.test/");
        Assert.True(await provider.IsEnabledAsync("Elsa.A"));
        Assert.False(await provider.IsEnabledAsync("Elsa.B"));
        Assert.Equal(3, api.ListCalls);
    }

    private static ListResponse<FeatureDescriptor> FeaturesFor(string fullName) =>
        new([new FeatureDescriptor { FullName = fullName }], 1);

    private sealed class BackendApiClientProvider(IFeaturesApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; set; } = new("https://elsa.example.test/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromResult((T)api);
    }

    private sealed class FeaturesApi : IFeaturesApi
    {
        public static ListResponse<FeatureDescriptor> InstalledFeatures { get; } = new(
            [new FeatureDescriptor { FullName = "Elsa.ExternalAuthentication" }],
            1);

        public Queue<Func<CancellationToken, Task<ListResponse<FeatureDescriptor>>>> Responses { get; } = new();
        public int GetCalls { get; private set; }
        public int ListCalls { get; private set; }

        public Task<FeatureDescriptor> GetAsync(string fullName, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            throw new NotSupportedException();
        }

        public Task<ListResponse<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCalls++;
            if (Responses.TryDequeue(out var response))
                return response(cancellationToken);

            return Task.FromResult(InstalledFeatures);
        }
    }

    private sealed class AuthenticationStateProviderStub : AuthenticationStateProvider
    {
        public bool IsAuthenticated { get; set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = IsAuthenticated
                ? new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "test")
                : new ClaimsIdentity();
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }
}
