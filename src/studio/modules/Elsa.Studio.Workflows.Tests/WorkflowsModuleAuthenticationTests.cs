using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Client;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Regression coverage for elsa-workflows/elsa-studio#1034: <c>AddWorkflowsModule()</c> registered
/// <see cref="IBpmnInterchangeApi"/>'s Refit client without a <see cref="BackendApiConfig"/>, so it never picked up
/// the host's <c>AuthenticationHandler</c> and every BPMN call came back 401 even though the rest of the session was
/// authenticated. The existing tests around <see cref="Domain.Services.RemoteBpmnInterchangeService"/> fake
/// <see cref="IBackendApiClientProvider"/> directly, so they never exercised the real HTTP pipeline this bug lived
/// in; this test wires the module the same way a host does and inspects what the pipeline actually sends.
/// </summary>
public class WorkflowsModuleAuthenticationTests : IDisposable
{
    private readonly RecordingHandler _primaryHandler = new();
    private readonly ServiceProvider _serviceProvider;

    public WorkflowsModuleAuthenticationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();

        var backendApiConfig = new BackendApiConfig
        {
            ConfigureBackendOptions = options => options.Url = new Uri("https://backend.example"),
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(StampingAuthenticationHandler)
        };

        services.AddRemoteBackend(backendApiConfig);
        services.AddWorkflowsModule(backendApiConfig);

        // Swap in a stub transport so the assertion can see what actually went out, without a real server.
        services.AddHttpClient(nameof(IBpmnInterchangeApi)).ConfigurePrimaryHttpMessageHandler(() => _primaryHandler);

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public async Task BpmnInterchangeApi_CarriesTheHostsConfiguredAuthenticationHandler()
    {
        var provider = _serviceProvider.GetRequiredService<IBackendApiClientProvider>();
        var api = await provider.GetApiAsync<IBpmnInterchangeApi>();

        await api.ExportAsync("wf-1");

        Assert.NotNull(_primaryHandler.LastRequest);
        Assert.Equal("stamped", _primaryHandler.LastRequest!.Headers.GetValues(StampingAuthenticationHandler.HeaderName).Single());
    }

    /// <summary>
    /// Stands in for a host's real authentication handler (e.g. <c>AuthenticatingApiHttpMessageHandler</c>): it stamps
    /// every outgoing request, so its absence from the pipeline is observable.
    /// </summary>
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });
        }
    }
}
