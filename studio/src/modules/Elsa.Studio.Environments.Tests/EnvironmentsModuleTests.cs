using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Environments.Models;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Environments.Tests;

/// <summary>
/// Regression coverage for elsa-workflows/elsa-studio#1039: Environments must switch backends by
/// updating <see cref="IRemoteBackendAccessor"/> and reusing <see cref="DefaultBackendApiClientProvider"/>,
/// not by replacing it with a private ServiceProvider factory.
/// </summary>
/// <remarks>
/// Also covers #1062: Blazor Server host startup must not call the environments client.
/// The list loads once from <see cref="IFeatureService.InitializeFeaturesAsync"/>.
/// </remarks>
public class EnvironmentsModuleTests : IDisposable
{
    private readonly RecordingHandler _primaryHandler = new();
    private readonly ServiceProvider _serviceProvider;

    public EnvironmentsModuleTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();
        services.AddSingleton<IRemoteFeatureProvider, EmptyRemoteFeatureProvider>();

        var backendApiConfig = new BackendApiConfig
        {
            ConfigureBackendOptions = options => options.Url = new Uri("https://backend.example/"),
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(StampingAuthenticationHandler)
        };

        services.AddRemoteBackend(backendApiConfig);
        services.AddEnvironmentsModule(backendApiConfig);
        services.AddHttpClient(nameof(IEnvironmentsClient)).ConfigurePrimaryHttpMessageHandler(() => _primaryHandler);

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public void AddEnvironmentsModule_KeepsDefaultBackendApiClientProvider()
    {
        var provider = _serviceProvider.GetRequiredService<IBackendApiClientProvider>();
        var accessor = _serviceProvider.GetRequiredService<IRemoteBackendAccessor>();

        Assert.IsType<DefaultBackendApiClientProvider>(provider);
        Assert.IsType<EnvironmentRemoteBackendAccessor>(accessor);
        Assert.Null(typeof(EnvironmentRemoteBackendAccessor).Assembly.GetType(
            "Elsa.Studio.Environments.Services.EnvironmentBackendApiClientProvider"));
    }

    [Fact]
    public void AddEnvironmentsModule_DoesNotRegisterStartupTask()
    {
        Assert.Empty(_serviceProvider.GetServices<IStartupTask>());
    }

    [Fact]
    public async Task BlazorServerStartupPath_DoesNotCallEnvironmentsClient()
    {
        // Arrange: same path as RunStartupTasksHostedService during Host.StartAsync.
        var runner = _serviceProvider.GetRequiredService<IStartupTaskRunner>();

        // Act
        await runner.RunStartupTasksAsync();

        // Assert
        Assert.Null(_primaryHandler.LastRequest);
        Assert.Equal(0, _primaryHandler.RequestCount);
        Assert.Empty(_serviceProvider.GetRequiredService<IEnvironmentService>().Environments);
    }

    [Fact]
    public async Task FeatureInitialize_OnlyAddsThePicker()
    {
        var feature = _serviceProvider.GetServices<IFeature>().OfType<Feature>().Single();

        await feature.InitializeAsync();

        Assert.Equal(0, _primaryHandler.RequestCount);
        Assert.Empty(_serviceProvider.GetRequiredService<IEnvironmentService>().Environments);
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task InitializeFeatures_LoadsEnvironmentsOnce()
    {
        // Arrange
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Dev", "url": "https://dev.example/" },
                { "name": "Prod", "url": "https://prod.example/" }
              ],
              "defaultEnvironmentName": "Dev"
            }
            """;

        var features = _serviceProvider.GetRequiredService<IFeatureService>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act
        await features.InitializeFeaturesAsync();
        await features.InitializeFeaturesAsync();

        // Assert
        Assert.Equal(1, _primaryHandler.RequestCount);
        Assert.Equal(["Dev", "Prod"], environments.Environments.Select(environment => environment.Name));
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
        Assert.Equal(new Uri("https://dev.example/"), environments.CurrentEnvironment?.Url);
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task EnvironmentLoader_EnsureLoadedAsync_LoadsOnce()
    {
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Staging", "url": "https://staging.example/" }
              ],
              "defaultEnvironmentName": "Staging"
            }
            """;

        var loader = _serviceProvider.GetRequiredService<EnvironmentLoader>();

        await loader.EnsureLoadedAsync();
        await loader.EnsureLoadedAsync();

        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        Assert.Equal(1, _primaryHandler.RequestCount);
        Assert.Equal("Staging", environments.CurrentEnvironment?.Name);
        Assert.Contains(environments.Environments, environment => environment.Name == "Staging");
    }

    [Fact]
    public async Task EnvironmentLoader_FailedLoad_IsNotCachedAndRetries()
    {
        // Arrange
        _primaryHandler.ThrowOnSend = new InvalidOperationException("The environments API is unreachable.");
        var loader = _serviceProvider.GetRequiredService<EnvironmentLoader>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.Empty(environments.Environments);
        var failedRequests = _primaryHandler.RequestCount;
        Assert.True(failedRequests >= 1);

        // Arrange: backend recovers
        _primaryHandler.ThrowOnSend = null;
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Dev", "url": "https://dev.example/" }
              ],
              "defaultEnvironmentName": "Dev"
            }
            """;

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.True(_primaryHandler.RequestCount > failedRequests);
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
    }

    [Fact]
    public async Task EnvironmentLoader_HttpClientTimeout_DoesNotThrowAndRetries()
    {
        // Arrange: HttpClient.Timeout surfaces as TaskCanceledException while the caller token is still open.
        _primaryHandler.ThrowOnSend = new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout.");
        var loader = _serviceProvider.GetRequiredService<EnvironmentLoader>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.Empty(environments.Environments);
        var timedOutRequests = _primaryHandler.RequestCount;
        Assert.True(timedOutRequests >= 1);

        // Arrange: backend recovers
        _primaryHandler.ThrowOnSend = null;
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Dev", "url": "https://dev.example/" }
              ],
              "defaultEnvironmentName": "Dev"
            }
            """;

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.True(_primaryHandler.RequestCount > timedOutRequests);
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
    }

    [Fact]
    public async Task EnvironmentLoader_CallerCancellation_PropagatesAndIsNotCached()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var loader = _serviceProvider.GetRequiredService<EnvironmentLoader>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loader.EnsureLoadedAsync(cts.Token));
        Assert.Empty(environments.Environments);

        // Arrange: a later uncancelled call must still load.
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Dev", "url": "https://dev.example/" }
              ],
              "defaultEnvironmentName": "Dev"
            }
            """;

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.True(_primaryHandler.RequestCount >= 1);
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
    }

    [Fact]
    public async Task InitializeFeatures_FillsEnvironmentsForThePicker()
    {
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Staging", "url": "https://staging.example/" }
              ],
              "defaultEnvironmentName": "Staging"
            }
            """;

        await _serviceProvider.GetRequiredService<IFeatureService>().InitializeFeaturesAsync();

        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        Assert.Equal("Staging", environments.CurrentEnvironment?.Name);
        Assert.Contains(environments.Environments, environment => environment.Name == "Staging");
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task InitializeFeatures_FailedLoad_MakesOneAttempt()
    {
        // Arrange
        _primaryHandler.ThrowOnSend = new InvalidOperationException("The environments API is unreachable.");
        var features = _serviceProvider.GetRequiredService<IFeatureService>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act
        await features.InitializeFeaturesAsync();

        // Assert
        Assert.Equal(1, _primaryHandler.RequestCount);
        Assert.Empty(environments.Environments);
        Assert.Null(environments.CurrentEnvironment);
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task InitializeFeatures_DoesNotThrowWhenEnvironmentsApiIsMissing()
    {
        _primaryHandler.StatusCode = HttpStatusCode.NotFound;

        await _serviceProvider.GetRequiredService<IFeatureService>().InitializeFeaturesAsync();

        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        Assert.Empty(environments.Environments);
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task SwitchingEnvironment_UpdatesAccessorUrlUsedByDefaultProvider()
    {
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        environments.SetEnvironments([
            new ServerEnvironment { Name = "Dev", Url = new Uri("https://dev.example/") },
            new ServerEnvironment { Name = "Prod", Url = new Uri("https://prod.example/") }
        ], "Dev");

        var accessor = _serviceProvider.GetRequiredService<IRemoteBackendAccessor>();
        var provider = _serviceProvider.GetRequiredService<IBackendApiClientProvider>();

        Assert.Equal(new Uri("https://dev.example/"), accessor.RemoteBackend.Url);
        Assert.Equal(new Uri("https://dev.example/"), provider.Url);

        environments.SetCurrentEnvironment("Prod");

        Assert.Equal(new Uri("https://prod.example/"), accessor.RemoteBackend.Url);
        Assert.Equal(new Uri("https://prod.example/"), provider.Url);

        var api = await provider.GetApiAsync<IEnvironmentsClient>();
        await api.ListEnvironmentsAsync();
        Assert.StartsWith("https://prod.example/", _primaryHandler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task EnvironmentsClient_CarriesTheHostsConfiguredAuthenticationHandler()
    {
        _primaryHandler.ResponseContent = """{ "environments": [], "defaultEnvironmentName": null }""";

        var provider = _serviceProvider.GetRequiredService<IBackendApiClientProvider>();
        var api = await provider.GetApiAsync<IEnvironmentsClient>();
        await api.ListEnvironmentsAsync();

        Assert.NotNull(_primaryHandler.LastRequest);
        Assert.Equal("stamped", _primaryHandler.LastRequest!.Headers.GetValues(StampingAuthenticationHandler.HeaderName).Single());
    }

    private sealed class StampingAuthenticationHandler : DelegatingHandler
    {
        public const string HeaderName = "X-Test-Authenticated";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(HeaderName, "stamped");
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class EmptyRemoteFeatureProvider : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    internal sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public int RequestCount { get; private set; }
        public List<HttpRequestMessage> Requests { get; } = [];
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string ResponseContent { get; set; } = """{ "environments": [], "defaultEnvironmentName": null }""";
        public Exception? ThrowOnSend { get; set; }
        public int RemainingEnvironmentFailures { get; set; }
        public Func<HttpRequestMessage, string>? ResponseContentForRequest { get; set; }

        public int EnvironmentRequestCount =>
            Requests.Count(request => request.RequestUri?.AbsolutePath.Contains("environments", StringComparison.OrdinalIgnoreCase) == true);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastRequest = request;
            RequestCount++;
            Requests.Add(request);

            if (ThrowOnSend is not null)
                throw ThrowOnSend;

            if (RemainingEnvironmentFailures > 0
                && request.RequestUri?.AbsolutePath.Contains("environments", StringComparison.OrdinalIgnoreCase) == true)
            {
                RemainingEnvironmentFailures--;
                throw new InvalidOperationException("The environments API is unreachable.");
            }

            var content = ResponseContentForRequest?.Invoke(request) ?? ResponseContent;

            return Task.FromResult(new HttpResponseMessage(StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(content, Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
            });
        }
    }
}
