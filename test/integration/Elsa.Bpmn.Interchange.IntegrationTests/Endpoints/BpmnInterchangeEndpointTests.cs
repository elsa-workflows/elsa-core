using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Elsa;
using Elsa.Bpmn.Interchange.Features;
using Elsa.Bpmn.Interchange.IntegrationTests.Support;
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

        var response = await PostAuthenticatedAsync("bpmn/analyze", content, "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Analyze_WithTwoFiles_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "first");
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "second");

        var response = await PostAuthenticatedAsync("bpmn/analyze", content, "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Analyze_WithOneValidFile_ReturnsOkWithAnalysis()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/analyze", content, "workflows/definitions:view");

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

        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Import_OfAValidDocument_ReturnsOkAndPersistsADefinition()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("definitionId").GetString()));
    }

    [Fact]
    public async Task Export_OfAMissingDefinition_ReturnsNotFound()
    {
        var response = await GetAuthenticatedAsync("bpmn/definitions/does-not-exist/export", "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_OfADefinitionNeverImportedFromBpmn_ReturnsUnprocessableEntity()
    {
        var definitionId = await CreateNonBpmnDefinitionAsync();

        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("does not currently carry BPMN source", body);
    }

    [Fact]
    public async Task Export_WithAMalformedVersion_ReturnsBadRequest()
    {
        var response = await GetAuthenticatedAsync("bpmn/definitions/does-not-exist/export?VersionOptions=not-a-number", "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("not-a-number", body);
    }

    [Fact]
    public async Task Export_OfAFreshlyImportedDefinition_ReturnsOkWithBpmnXml()
    {
        using var importContent = new MultipartFormDataContent();
        AddBpmnFile(importContent, ReadAsset("camunda-order-process.bpmn"), "file");
        var importResponse = await PostAuthenticatedAsync("bpmn/import", importContent, "workflows/definitions:write");
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        using var importDocument = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        var definitionId = importDocument.RootElement.GetProperty("definitionId").GetString();

        var exportResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "workflows/definitions:view");

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
        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DocumentGet_OfAMissingDefinition_ReturnsNotFound()
    {
        var response = await GetAuthenticatedAsync("bpmn/definitions/does-not-exist/document", "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DocumentGet_OfADefinitionNeverImportedFromBpmn_ReturnsUnprocessableEntity()
    {
        var definitionId = await CreateNonBpmnDefinitionAsync();

        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("does not currently carry BPMN source", body);
    }

    [Fact]
    public async Task DocumentGet_OfAFreshlyImportedDefinition_ReturnsOkWithTheLibraryFormatDocument()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();

        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Contains("order-process", document.RootElement.GetProperty("processes").EnumerateArray().Select(process => process.GetProperty("processId").GetString()));
    }

    [Fact]
    public async Task DocumentGet_WhenAuthenticatedWithoutTheRequiredPermission_ReturnsForbidden()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();

        // Authenticated, but only holds the write permission Put needs, not the read permission Get needs.
        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:write");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DocumentPut_OfAMissingDefinition_ReturnsNotFound()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await PutAuthenticatedAsync("bpmn/definitions/does-not-exist/document", content, "workflows/definitions:write");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DocumentPut_WithMalformedJson_ReturnsBadRequestAndPersistsNoNewDraft()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        using var content = new StringContent("{ not valid json", Encoding.UTF8, "application/json");
        var response = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", content, "workflows/definitions:write");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(versionBeforePut, await LatestVersionOfAsync(definitionId));
    }

    [Fact]
    public async Task DocumentPut_ThatRemovesARequiredActivityBinding_ReturnsUnprocessableEntityAndPersistsNoNewDraft()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var documentJson = await getResponse.Content.ReadAsStringAsync();

        // Strips the one <elsa:activityBinding> the document carries (on "NotifyWarehouse"), reproducing exactly
        // what Import itself refuses for unbound-task-process.bpmn: a task-family element the document describes
        // but does not say how to perform.
        using var editedDocument = JsonDocument.Parse(documentJson);
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteWithoutActivityBindingExtensions(editedDocument.RootElement, writer);
        }

        using var putContent = new ByteArrayContent(stream.ToArray());
        putContent.Headers.ContentType = new("application/json");

        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, "workflows/definitions:write");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, putResponse.StatusCode);
        var body = await putResponse.Content.ReadAsStringAsync();
        Assert.Contains("nothing binds it to an Elsa activity", body);
        Assert.Equal(versionBeforePut, await LatestVersionOfAsync(definitionId));
    }

    [Fact]
    public async Task DocumentPut_UnchangedDocument_ReturnsOkAndTheSameFindingsAsImport()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var documentJson = await getResponse.Content.ReadAsStringAsync();

        using var putContent = new StringContent(documentJson, Encoding.UTF8, "application/json");
        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, "workflows/definitions:write");

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        using var putResult = JsonDocument.Parse(await putResponse.Content.ReadAsStringAsync());
        Assert.Equal(definitionId, putResult.RootElement.GetProperty("definitionId").GetString());
        Assert.Contains(
            "order-process",
            putResult.RootElement.GetProperty("analysis").GetProperty("processIds").EnumerateArray().Select(processId => processId.GetString()));

        var exportResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "workflows/definitions:view");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        var exportedXml = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("order-process", exportedXml);
    }

    [Fact]
    public async Task DocumentPut_WhenAuthenticatedWithoutTheRequiredPermission_ReturnsForbidden()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Authenticated, but only holds the read permission Get needs, not the write permission Put needs.
        var response = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", content, "workflows/definitions:view");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DocumentPut_ForASingleProcessDefinitionImportedBeforeSourceProcessIdExisted_ReturnsOk()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        // Simulates a definition imported before ImportAsync started recording SourceProcessIdCustomPropertyKey.
        await RemoveSourceProcessIdCustomPropertyAsync(definitionId);

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var documentJson = await getResponse.Content.ReadAsStringAsync();

        using var putContent = new StringContent(documentJson, Encoding.UTF8, "application/json");
        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, "workflows/definitions:write");

        // A single-process document does not need SourceProcessId to disambiguate anything, so the missing
        // property does not stop the edit from succeeding.
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
    }

    [Fact]
    public async Task DocumentPut_ForATwoProcessDefinitionImportedBeforeSourceProcessIdExisted_ReturnsBadRequestAndPersistsNoNewDraft()
    {
        var definitionId = await ImportTwoProcessDocumentAsync("first-process");
        // Simulates a definition imported before ImportAsync started recording SourceProcessIdCustomPropertyKey:
        // without it, Put has no way to know which of the document's two processes to re-bind.
        await RemoveSourceProcessIdCustomPropertyAsync(definitionId);
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var documentJson = await getResponse.Content.ReadAsStringAsync();

        using var putContent = new StringContent(documentJson, Encoding.UTF8, "application/json");
        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, "workflows/definitions:write");

        Assert.True((int)putResponse.StatusCode is >= 400 and < 500, $"Expected a 4xx status code, got {(int)putResponse.StatusCode}.");
        var body = await putResponse.Content.ReadAsStringAsync();
        Assert.Contains("specify which one to import", body);
        Assert.Equal(versionBeforePut, await LatestVersionOfAsync(definitionId));
    }

    /// <summary>
    /// Re-serializes <paramref name="element"/> with every <c>extensionElements</c> array entry named
    /// <c>activityBinding</c> (in the <c>elsa:</c> namespace URI) removed, walking the whole document recursively.
    /// </summary>
    private static void WriteWithoutActivityBindingExtensions(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteWithoutActivityBindingExtensions(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    if (IsActivityBindingExtensionElement(item))
                        continue;

                    WriteWithoutActivityBindingExtensions(item, writer);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static bool IsActivityBindingExtensionElement(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("name", out var name)
        && name.ValueKind == JsonValueKind.Object
        && name.TryGetProperty("localName", out var localName)
        && localName.GetString() == "activityBinding"
        && name.TryGetProperty("ns", out var ns)
        && ns.GetString() == "https://elsaworkflows.io/schemas/bpmn/v1";

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

    private static string ReadAsset(string fileName) => BpmnAssetReader.Read(fileName);

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

    private async Task<HttpResponseMessage> PutAuthenticatedAsync(string requestUri, HttpContent content, params string[] permissions)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
        request.Headers.Add(TestAuthenticationHandler.PermissionHeader, string.Join(",", permissions));
        return await HttpClient.SendAsync(request);
    }

    /// <summary>Imports <c>camunda-order-process.bpmn</c> through the real endpoint and returns the resulting <c>definitionId</c>.</summary>
    private async Task<string> ImportCamundaOrderProcessAsync()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");
        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("definitionId").GetString()!;
    }

    /// <summary>Imports <c>two-process.bpmn</c>, picking <paramref name="processId"/>, and returns the resulting <c>definitionId</c>.</summary>
    private async Task<string> ImportTwoProcessDocumentAsync(string processId)
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("two-process.bpmn"), "file");
        content.Add(new StringContent(processId), "ProcessId");
        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("definitionId").GetString()!;
    }

    /// <summary>
    /// Removes <see cref="BpmnInterchangeDocumentService.SourceProcessIdCustomPropertyKey"/> from the latest version of
    /// <paramref name="definitionId"/>, simulating a definition imported before <c>ImportAsync</c> started recording it.
    /// </summary>
    private async Task RemoveSourceProcessIdCustomPropertyAsync(string definitionId)
    {
        using var scope = _app!.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await store.FindAsync(filter);
        Assert.NotNull(definition);
        definition!.CustomProperties.Remove(BpmnInterchangeDocumentService.SourceProcessIdCustomPropertyKey);
        await store.SaveAsync(definition);
    }

    private async Task<int> LatestVersionOfAsync(string definitionId)
    {
        using var scope = _app!.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await store.FindAsync(filter);
        Assert.NotNull(definition);
        return definition!.Version;
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
