using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Elsa;
using Elsa.Bpmn.Interchange.Features;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Endpoints;

/// <summary>
/// HTTP-level coverage for the Analyze/Import/Export endpoints: the wrapper-specific logic this whole program is
/// named after — multipart file-count validation, exception-to-status-code mapping, and permission gating — none of
/// which <see cref="Scenarios.Interchange.BpmnInterchangeDocumentServiceTests"/> or
/// <see cref="Scenarios.Interchange.BpmnExportAvailabilityTests"/> exercise, since those call the service directly.
/// Follows the precedent in <c>Elsa.Resilience.IntegrationTests.SimulateResponseEndpointTests</c>: a real
/// <see cref="WebApplication"/> with FastEndpoints and a <see cref="TestServer"/>, so status codes and permission
/// gating are asserted against real HTTP responses rather than an endpoint invoked in isolation.
/// </summary>
[Collection(nameof(BpmnInterchangeEndpointCollection))]
public class BpmnInterchangeEndpointTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private WebApplication? _app;
    private bool _wasSecurityEnabled;

    private HttpClient HttpClient { get; set; } = null!;

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = true;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationScheme, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(BpmnInterchangeFeature).Assembly];
            o.DisableAutoDiscovery = true;
        });
        builder.Services.AddLogging(logging => logging.AddProvider(new XunitLoggerProvider(testOutputHelper)));
        builder.Services.AddElsa(elsa => elsa
            .AddActivitiesFrom<WriteLine>()
            .UseScheduling()
            .UseCSharp(options => options.AllowHostCodeExecution = true)
            .UseJavaScript()
            .UseLiquid()
            .UseWorkflowManagement()
            .UseBpmnInterchange());

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();

        await _app.StartAsync();
        await _app.Services.PopulateRegistriesAsync();
        HttpClient = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        HttpClient.Dispose();

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Analyze_WithZeroFiles_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        // A well-formed multipart body with no file part: an empty MultipartFormDataContent serializes a section
        // whose Content-Disposition ASP.NET Core's form parser rejects outright, which would fail before the
        // endpoint's own zero-files check ever runs.
        content.Add(new StringContent("value"), "field");

        var response = await PostAuthenticatedAsync("bpmn/analyze", content, "read:workflow-definitions");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Analyze_WithTwoFiles_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "first");
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "second");

        var response = await PostAuthenticatedAsync("bpmn/analyze", content, "read:workflow-definitions");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Analyze_WithOneValidFile_ReturnsOkWithAnalysis()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/analyze", content, "read:workflow-definitions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Contains("order-process", document.RootElement.GetProperty("processIds").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Import_OfADocumentWithAnUnboundTask_ReturnsUnprocessableEntity()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("unbound-task-process.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/import", content, "write:workflow-definitions");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Import_OfAValidDocument_ReturnsOkAndPersistsADefinition()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/import", content, "write:workflow-definitions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("definitionId").GetString()));
    }

    [Fact]
    public async Task Export_OfAMissingDefinition_ReturnsNotFound()
    {
        var response = await GetAuthenticatedAsync("bpmn/definitions/does-not-exist/export", "read:workflow-definitions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_OfADefinitionNeverImportedFromBpmn_ReturnsUnprocessableEntity()
    {
        var definitionId = await CreateNonBpmnDefinitionAsync();

        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "read:workflow-definitions");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("does not currently carry BPMN source", body);
    }

    [Fact]
    public async Task Export_OfAFreshlyImportedDefinition_ReturnsOkWithBpmnXml()
    {
        using var importContent = new MultipartFormDataContent();
        AddBpmnFile(importContent, ReadAsset("camunda-order-process.bpmn"), "file");
        var importResponse = await PostAuthenticatedAsync("bpmn/import", importContent, "write:workflow-definitions");
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        using var importDocument = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        var definitionId = importDocument.RootElement.GetProperty("definitionId").GetString();

        var exportResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "read:workflow-definitions");

        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        var exportedXml = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("order-process", exportedXml);
    }

    [Fact]
    public async Task Export_WhenUnauthenticated_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "bpmn/definitions/does-not-exist/export");
        var response = await HttpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Import_WhenAuthenticatedWithoutTheRequiredPermission_ReturnsForbidden()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");

        // Authenticated, but only holds the read permission Export needs, not the write permission Import needs.
        var response = await PostAuthenticatedAsync("bpmn/import", content, "read:workflow-definitions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> CreateNonBpmnDefinitionAsync()
    {
        using var scope = _app!.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var definitionId = Guid.NewGuid().ToString();

        await store.SaveAsync(new WorkflowDefinition
        {
            Id = Guid.NewGuid().ToString(),
            DefinitionId = definitionId,
            Name = "Not BPMN",
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json",
            StringData = "{}"
        });

        return definitionId;
    }

    private static void AddBpmnFile(MultipartFormDataContent content, string xml, string formFieldName)
    {
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(xml));
        fileContent.Headers.ContentType = new("application/xml");
        content.Add(fileContent, formFieldName, $"{formFieldName}.bpmn");
    }

    private static string ReadAsset(string fileName) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", fileName));

    private async Task<HttpResponseMessage> PostAuthenticatedAsync(string requestUri, HttpContent content, params string[] permissions)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
        request.Headers.Add(TestAuthenticationHandler.PermissionHeader, string.Join(",", permissions));
        return await HttpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(string requestUri, params string[] permissions)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add(TestAuthenticationHandler.PermissionHeader, string.Join(",", permissions));
        return await HttpClient.SendAsync(request);
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "Test";
        public const string PermissionHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PermissionHeader, out var permissionHeader))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = permissionHeader
                .SelectMany(x => x?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
                .Select(x => new Claim(PermissionNames.ClaimType, x))
                .ToList();

            claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user"));

            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

[CollectionDefinition(nameof(BpmnInterchangeEndpointCollection), DisableParallelization = true)]
public class BpmnInterchangeEndpointCollection;
