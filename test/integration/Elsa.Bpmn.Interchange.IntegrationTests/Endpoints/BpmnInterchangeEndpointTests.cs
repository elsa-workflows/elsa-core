using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Interchange.Features;
using Elsa.Bpmn.Interchange.IntegrationTests.Support;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Memory;
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

        var response = await PutAuthenticatedAsync("bpmn/definitions/does-not-exist/document", content, null, "workflows/definitions:write");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DocumentPut_WithoutIfMatch_ReturnsPreconditionRequired()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", content, null, "workflows/definitions:write");

        Assert.Equal((HttpStatusCode)428, response.StatusCode);
        Assert.Equal(versionBeforePut, await LatestVersionOfAsync(definitionId));
    }

    [Fact]
    public async Task DocumentPut_WithAWildcardIfMatch_ReturnsPreconditionRequiredAndOverwritesNothing()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var (_, documentJson) = await GetDocumentAsync(definitionId);
        var storedBeforePut = await LatestStoredAsync(definitionId);

        // "*" matches whatever is currently stored, so honouring it would be exactly the blind overwrite the required
        // If-Match exists to prevent.
        var response = await PutDocumentAsync(definitionId, WithFirstShapeMoved(documentJson), "*");

        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        Assert.Equal(storedBeforePut, await LatestStoredAsync(definitionId));
    }

    [Fact]
    public async Task DocumentPut_WithTheCurrentIfMatch_ReturnsOkWithANewETagWhenTheContentChanged()
    {
        var definitionId = await ImportCamundaOrderProcessWrittenBackAsync();
        var (currentETag, documentJson) = await GetDocumentAsync(definitionId);

        var putResponse = await PutDocumentAsync(definitionId, WithFirstShapeMoved(documentJson), currentETag);

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var newETag = ETagOf(putResponse);
        Assert.NotNull(newETag);
        Assert.NotEqual(currentETag, newETag);
        // The returned ETag names what was stored, so it is exactly what the next GET hands out.
        Assert.Equal(newETag, (await GetDocumentAsync(definitionId)).ETag);
    }

    [Fact]
    public async Task DocumentPut_OfUnchangedContent_ReturnsTheETagTheGetReturned()
    {
        var definitionId = await ImportCamundaOrderProcessWrittenBackAsync();
        var (currentETag, documentJson) = await GetDocumentAsync(definitionId);

        var putResponse = await PutDocumentAsync(definitionId, documentJson, currentETag);

        // Content-addressed: writing back exactly what is stored overwrites nothing, so the validator stays the same.
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        Assert.Equal(currentETag, ETagOf(putResponse));
    }

    [Fact]
    public async Task DocumentPut_AfterAnInterveningDocumentPut_ReturnsPreconditionFailedAndOverwritesNothing()
    {
        var definitionId = await ImportCamundaOrderProcessWrittenBackAsync();
        var (staleETag, documentJson) = await GetDocumentAsync(definitionId);
        var storedBeforeInterveningPut = await LatestStoredAsync(definitionId);

        var interveningPut = await PutDocumentAsync(definitionId, WithFirstShapeMoved(documentJson), staleETag);
        Assert.Equal(HttpStatusCode.OK, interveningPut.StatusCode);

        // A layout-only edit to an unpublished draft: same row, same version, same activity graph — only the stored
        // document moved, so this is the case that proves the document itself is part of the ETag.
        var storedAfterInterveningPut = await LatestStoredAsync(definitionId);
        Assert.Equal(storedBeforeInterveningPut with { SourceXml = storedAfterInterveningPut.SourceXml }, storedAfterInterveningPut);
        Assert.NotEqual(storedBeforeInterveningPut.SourceXml, storedAfterInterveningPut.SourceXml);

        await AssertStalePutIsRefusedAsync(definitionId, documentJson, staleETag);
    }

    [Fact]
    public async Task DocumentPut_AfterAnInterveningImportIntoTheSameDefinition_ReturnsPreconditionFailedAndOverwritesNothing()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var (staleETag, documentJson) = await GetDocumentAsync(definitionId);
        var storedBeforeImport = await LatestStoredAsync(definitionId);

        // A different document declaring the same process, so the stale PUT below would bind cleanly and silently
        // replace it if the precondition let it through.
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn").Replace("Order Handled", "Order Shipped"), "file");
        content.Add(new StringContent(definitionId), "DefinitionId");
        var importResponse = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        // Imported into the unpublished draft in place: same row, same version.
        var storedAfterImport = await LatestStoredAsync(definitionId);
        Assert.Equal((storedBeforeImport.Id, storedBeforeImport.Version), (storedAfterImport.Id, storedAfterImport.Version));

        await AssertStalePutIsRefusedAsync(definitionId, documentJson, staleETag);
    }

    [Fact]
    public async Task DocumentPut_AfterAnInterveningDesignerSaveOfTheDraft_ReturnsPreconditionFailedAndOverwritesNothing()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var (staleETag, documentJson) = await GetDocumentAsync(definitionId);
        var storedBeforeSave = await LatestStoredAsync(definitionId);

        await SaveDraftFromTheDesignerAsync(definitionId);

        // Saved in place with every custom property carried forward: same row, same version, same stored document —
        // only the activity graph moved, so this is the case that proves the graph itself is part of the ETag.
        var storedAfterSave = await LatestStoredAsync(definitionId);
        Assert.Equal(storedBeforeSave with { StringData = storedAfterSave.StringData }, storedAfterSave);
        Assert.NotEqual(storedBeforeSave.StringData, storedAfterSave.StringData);

        await AssertStalePutIsRefusedAsync(definitionId, documentJson, staleETag);
    }

    [Fact]
    public async Task DocumentPut_WithMalformedJson_ReturnsBadRequestAndPersistsNoNewDraft()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var content = new StringContent("{ not valid json", Encoding.UTF8, "application/json");
        var response = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", content, ETagOf(getResponse), "workflows/definitions:write");

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

        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, ETagOf(getResponse), "workflows/definitions:write");

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
        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, ETagOf(getResponse), "workflows/definitions:write");

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
    public async Task DocumentPut_WithABindingChange_PreservesTheDefinitionsNonBpmnMetadata()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        await SetNonBpmnMetadataOnDraftAsync(definitionId);
        var metadataBeforePut = await CaptureMetadataAsync(definitionId);

        var (etag, documentJson) = await GetDocumentAsync(definitionId);
        var putResponse = await PutDocumentAsync(definitionId, WithNotifyWarehouseTextChanged(documentJson), etag);

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // Every field SetNonBpmnMetadataOnDraftAsync set survived the PUT unchanged.
        var metadataAfterPut = await CaptureMetadataAsync(definitionId);
        Assert.Equal(metadataBeforePut, metadataAfterPut);

        // ...while the graph reflects the binding change the PUT carried.
        var exportResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "workflows/definitions:view");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        var exportedXml = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("Notifying the warehouse, rebound via document PUT", exportedXml);
    }

    [Fact]
    public async Task Import_WithTheSameDefinitionId_StillReplacesTheDefinitionsMetadata()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        await SetNonBpmnMetadataOnDraftAsync(definitionId);
        var metadataBeforeImport = await CaptureMetadataAsync(definitionId);

        // POST bpmn/import with a DefinitionId is a whole-definition import, unlike the document PUT above: it must
        // keep replacing everything SetNonBpmnMetadataOnDraftAsync set, exactly as it did before that PUT preserved
        // it.
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");
        content.Add(new StringContent(definitionId), "DefinitionId");
        var importResponse = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var metadataAfterImport = await CaptureMetadataAsync(definitionId);
        Assert.NotEqual(metadataBeforeImport, metadataAfterImport);
        Assert.Equal("Order Process", metadataAfterImport.Name);
        Assert.Null(metadataAfterImport.Description);
        Assert.Equal(string.Empty, metadataAfterImport.VariableSummary);
        Assert.Null(metadataAfterImport.UsableAsActivity);
        Assert.Equal(string.Empty, metadataAfterImport.InputSummary);
        Assert.Equal(string.Empty, metadataAfterImport.OutputSummary);
        Assert.Equal(string.Empty, metadataAfterImport.OutcomeSummary);
        Assert.Null(metadataAfterImport.ToolVersion);
        Assert.False(metadataAfterImport.IsReadonly);
        Assert.Null(metadataAfterImport.CustomPropertyValue);
    }

    [Fact]
    public async Task DocumentPut_WhenAuthenticatedWithoutTheRequiredPermission_ReturnsForbidden()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Authenticated, but only holds the read permission Get needs, not the write permission Put needs.
        var response = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", content, null, "workflows/definitions:view");

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
        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, ETagOf(getResponse), "workflows/definitions:write");

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
        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, ETagOf(getResponse), "workflows/definitions:write");

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
                foreach (var item in element.EnumerateArray().Where(item => !IsActivityBindingExtensionElement(item)))
                    WriteWithoutActivityBindingExtensions(item, writer);
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

    private async Task<HttpResponseMessage> PutAuthenticatedAsync(string requestUri, HttpContent content, string? ifMatch, params string[] permissions)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
        request.Headers.Add(TestAuthenticationHandler.PermissionHeader, string.Join(",", permissions));

        if (ifMatch is not null)
            request.Headers.Add("If-Match", ifMatch);

        return await HttpClient.SendAsync(request);
    }

    /// <summary>The ETag a prior <c>document</c> GET or PUT response carried, for use as the next PUT's <c>If-Match</c>.</summary>
    private static string? ETagOf(HttpResponseMessage response) => response.Headers.ETag?.Tag;

    /// <summary>GETs the document of <paramref name="definitionId"/>, asserting it succeeded, and returns its ETag and body.</summary>
    private async Task<(string? ETag, string Json)> GetDocumentAsync(string definitionId)
    {
        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (ETagOf(response), await response.Content.ReadAsStringAsync());
    }

    private Task<HttpResponseMessage> PutDocumentAsync(string definitionId, string documentJson, string? ifMatch) =>
        PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", new StringContent(documentJson, Encoding.UTF8, "application/json"), ifMatch, "workflows/definitions:write");

    /// <summary>
    /// PUTs <paramref name="documentJson"/> with <paramref name="staleETag"/> and asserts it is refused with 412 and leaves
    /// the stored definition exactly as it was. The version alone cannot show that: an unpublished draft is overwritten in
    /// place, under the same version, which is precisely the overwrite this is checking did not happen.
    /// </summary>
    private async Task AssertStalePutIsRefusedAsync(string definitionId, string documentJson, string? staleETag)
    {
        var storedBeforePut = await LatestStoredAsync(definitionId);

        var response = await PutDocumentAsync(definitionId, documentJson, staleETag);

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
        Assert.Equal(storedBeforePut, await LatestStoredAsync(definitionId));
    }

    /// <summary>Moves the document's first diagram shape to the right — a layout-only edit, the kind W14 makes.</summary>
    private static string WithFirstShapeMoved(string documentJson)
    {
        var document = JsonNode.Parse(documentJson)!;
        var bounds = document["diagrams"]![0]!["plane"]!["shapes"]![0]!["bounds"]!;
        bounds["x"] = bounds["x"]!.GetValue<double>() + 10;
        return document.ToJsonString();
    }

    /// <summary>
    /// Changes the literal text the <c>NotifyWarehouse</c> task's <c>elsa:activityBinding</c> configures its bound
    /// <see cref="WriteLine"/> with — a real binding change, the kind Elsa Studio's binding UX makes, as opposed to
    /// <see cref="WithFirstShapeMoved"/>'s layout-only edit.
    /// </summary>
    private static string WithNotifyWarehouseTextChanged(string documentJson) =>
        documentJson.Replace("Notifying the warehouse", "Notifying the warehouse, rebound via document PUT");

    /// <summary>
    /// Renames the latest draft of <paramref name="definitionId"/> and sets a description, a variable, an
    /// activity-usable option, an input, an output, an outcome, a tool version, the read-only flag and a custom
    /// property on it — the non-BPMN metadata a document PUT must leave untouched, set the way Studio's own
    /// definitions API would (<see cref="IWorkflowDefinitionPublisher.GetDraftAsync"/> then
    /// <see cref="IWorkflowDefinitionPublisher.SaveDraftAsync"/>).
    /// </summary>
    private async Task SetNonBpmnMetadataOnDraftAsync(string definitionId)
    {
        using var scope = _app!.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        var draft = await publisher.GetDraftAsync(definitionId, VersionOptions.Latest);
        Assert.NotNull(draft);

        draft!.Name = "Renamed by the author, not by BPMN";
        draft.Description = "Handles a customer order end to end.";
        draft.Variables = [new Variable<string>("OrderReference", "unset")];
        draft.Options.UsableAsActivity = true;
        draft.Inputs = [new InputDefinition { Name = "CustomerId", Type = typeof(string) }];
        draft.Outputs = [new OutputDefinition { Name = "OrderId", Type = typeof(string) }];
        draft.Outcomes = ["Fulfilled"];
        draft.ToolVersion = new Version(1, 2, 3);
        draft.IsReadonly = true;
        draft.CustomProperties["Custom:Owner"] = "fulfillment-team-lead";

        await publisher.SaveDraftAsync(draft);
    }

    /// <summary>A snapshot of everything <see cref="SetNonBpmnMetadataOnDraftAsync"/> sets, for before/after comparison.</summary>
    private sealed record CapturedMetadata(
        string? Name,
        string? Description,
        string VariableSummary,
        bool? UsableAsActivity,
        string InputSummary,
        string OutputSummary,
        string OutcomeSummary,
        Version? ToolVersion,
        bool IsReadonly,
        string? CustomPropertyValue);

    private async Task<CapturedMetadata> CaptureMetadataAsync(string definitionId)
    {
        var definition = await FindLatestDefinitionAsync(definitionId);
        var variableSummary = string.Join(";", definition.Variables.Select(variable => $"{variable.Name}={variable.Value}"));
        var inputSummary = string.Join(";", definition.Inputs.Select(input => $"{input.Name}:{input.Type}"));
        var outputSummary = string.Join(";", definition.Outputs.Select(output => $"{output.Name}:{output.Type}"));
        var outcomeSummary = string.Join(";", definition.Outcomes);
        definition.CustomProperties.TryGetValue<string>("Custom:Owner", out var customPropertyValue);

        return new(
            definition.Name,
            definition.Description,
            variableSummary,
            definition.Options.UsableAsActivity,
            inputSummary,
            outputSummary,
            outcomeSummary,
            definition.ToolVersion,
            definition.IsReadonly,
            customPropertyValue);
    }

    private async Task<WorkflowDefinition> FindLatestDefinitionAsync(string definitionId)
    {
        using var scope = _app!.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await store.FindAsync(filter);
        Assert.NotNull(definition);
        return definition!;
    }

    /// <summary>
    /// Saves the latest draft of <paramref name="definitionId"/> the way the workflow-definition save endpoint Studio's
    /// designer calls does — <see cref="IWorkflowDefinitionPublisher.GetDraftAsync"/>, a re-serialized root, then
    /// <see cref="IWorkflowDefinitionPublisher.SaveDraftAsync"/> — with the bound activity's text edited and every custom
    /// property, the stored BPMN source included, carried forward as Studio sends them back.
    /// </summary>
    private async Task SaveDraftFromTheDesignerAsync(string definitionId)
    {
        using var scope = _app!.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        var serializer = scope.ServiceProvider.GetRequiredService<IActivitySerializer>();
        var draft = await publisher.GetDraftAsync(definitionId, VersionOptions.Latest);
        Assert.NotNull(draft);

        var root = Assert.IsType<BpmnProcess>(serializer.Deserialize(draft!.StringData!));
        Assert.Single(root.Activities.OfType<WriteLine>()).Text = new("Notifying the warehouse, edited in the designer");
        draft.StringData = serializer.Serialize(root);

        await publisher.SaveDraftAsync(draft);
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

    /// <summary>
    /// Imports <c>camunda-order-process.bpmn</c> and writes its document straight back once through the document PUT, so
    /// what is stored is the writer's own rendering of it rather than the uploaded bytes. From there only an actual edit
    /// changes the stored content, which is what lets a test attribute an ETag change — or its absence — to one write.
    /// </summary>
    private async Task<string> ImportCamundaOrderProcessWrittenBackAsync()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var (etag, documentJson) = await GetDocumentAsync(definitionId);
        var response = await PutDocumentAsync(definitionId, documentJson, etag);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return definitionId;
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

    private async Task<int> LatestVersionOfAsync(string definitionId) => (await LatestStoredAsync(definitionId)).Version;

    /// <summary>
    /// A snapshot of the latest version of <paramref name="definitionId"/>: its id, version, activity graph and stored BPMN
    /// document — everything a document PUT rewrites that the document ETag covers.
    /// </summary>
    private async Task<StoredDefinition> LatestStoredAsync(string definitionId)
    {
        using var scope = _app!.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await store.FindAsync(filter);
        Assert.NotNull(definition);
        definition!.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey, out var sourceXml);
        return new(definition.Id, definition.Version, definition.StringData, sourceXml);
    }

    private sealed record StoredDefinition(string Id, int Version, string? StringData, string? SourceXml);

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
