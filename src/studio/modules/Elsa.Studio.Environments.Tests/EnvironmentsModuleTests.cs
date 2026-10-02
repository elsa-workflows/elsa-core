using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Environments.Models;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Environments.Tasks;
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
public class EnvironmentsModuleTests : IDisposable
{
    private readonly RecordingHandler _primaryHandler = new();
    private readonly ServiceProvider _serviceProvider;

    public EnvironmentsModuleTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();

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
    public async Task LoadEnvironmentsStartupTask_PopulatesEnvironmentService()
    {
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Dev", "url": "https://dev.example/" },
                { "name": "Prod", "url": "https://prod.example/" }
              ],
              "defaultEnvironmentName": "Dev"
            }
            """;

        var task = _serviceProvider.GetServices<IStartupTask>().OfType<LoadEnvironmentsStartupTask>().Single();
        await task.LoadAsync();

        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        Assert.Equal(["Dev", "Prod"], environments.Environments.Select(environment => environment.Name));
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
        Assert.Equal(new Uri("https://dev.example/"), environments.CurrentEnvironment?.Url);
    }

    [Fact]
    public async Task FeatureInitialize_FillsEnvironmentsForThePicker()
    {
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Staging", "url": "https://staging.example/" }
              ],
              "defaultEnvironmentName": "Staging"
            }
            """;

        var feature = _serviceProvider.GetServices<IFeature>().OfType<Feature>().Single();
        await feature.InitializeAsync();

        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        Assert.Equal("Staging", environments.CurrentEnvironment?.Name);
        Assert.Contains(environments.Environments, environment => environment.Name == "Staging");
    }

    [Fact]
    public async Task FeatureInitialize_DoesNotThrowWhenEnvironmentsApiIsMissing()
    {
        _primaryHandler.StatusCode = HttpStatusCode.NotFound;

        var feature = _serviceProvider.GetServices<IFeature>().OfType<Feature>().Single();
        await feature.InitializeAsync();

        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        Assert.Empty(environments.Environments);
        Assert.Contains(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements, _ => true);
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

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string ResponseContent { get; set; } = """{ "environments": [], "defaultEnvironmentName": null }""";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(ResponseContent, Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
            });
        }
    }
}
