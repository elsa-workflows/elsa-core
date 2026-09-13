using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Bpmn.Activities;
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
using Microsoft.Extensions.DependencyInjection;
using TUnit.AspNetCore;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Endpoints;

/// <summary>
/// HTTP-level coverage for the Analyze/Import/Export endpoints: the wrapper-specific logic this whole program is
/// named after — multipart file-count validation, exception-to-status-code mapping, and permission gating — none of
/// which <see cref="Scenarios.Interchange.BpmnInterchangeDocumentServiceTests"/> or
/// <see cref="Scenarios.Interchange.BpmnExportAvailabilityTests"/> exercise, since those call the service directly.
/// Uses TUnit.AspNetCore's isolated per-test application factory with FastEndpoints, so status codes and permission
/// gating are asserted against real HTTP responses rather than an endpoint invoked in isolation.
/// </summary>
public class BpmnInterchangeEndpointTests : WebApplicationTest<BpmnInterchangeWebApplicationFactory, BpmnInterchangeTestEntryPoint>
{
    private HttpClient? _httpClient;
    private HttpClient HttpClient => _httpClient ??= Factory.CreateClient();

    [Before(HookType.Test)]
    public Task PopulateRegistriesAsync() => Services.PopulateRegistriesAsync();

    [Test]
    public async Task Analyze_WithZeroFiles_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        // A well-formed multipart body with no file part: an empty MultipartFormDataContent serializes a section
        // whose Content-Disposition ASP.NET Core's form parser rejects outright, which would fail before the
        // endpoint's own zero-files check ever runs.
        content.Add(new StringContent("value"), "field");

        var response = await PostAuthenticatedAsync("bpmn/analyze", content, "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Analyze_WithTwoFiles_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "first");
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "second");

        var response = await PostAuthenticatedAsync("bpmn/analyze", content, "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Analyze_WithOneValidFile_ReturnsOkWithAnalysis()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/analyze", content, "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        await Assert.That(document.RootElement.GetProperty("processIds").EnumerateArray().Select(e => e.GetString())).Contains("order-process");
    }

    [Test]
    public async Task Import_OfADocumentWithAnUnboundTask_ReturnsUnprocessableEntity()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("unbound-task-process.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ImportBindingInvalid);
        await Assert.That(body).Contains("nothing binds it to an Elsa activity", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Import_OfADocumentWithASubprocessNestedInsideASubprocessThatReusesItsParentsId_ReturnsUnprocessableEntityAndTheServerStaysAlive()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("nested-subprocess-duplicate-id.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ImportDuplicateElementId);
        await Assert.That(body).Contains("Outer", StringComparison.CurrentCulture);

        // elsa-core#8074: before the fix, reading this document overflowed the stack and killed the process, which
        // .NET cannot catch — there would be no HTTP response to assert on at all. Reaching the assertions above already
        // proves the process survived; a further successful request proves the host is still serving requests, too.
        using var followUpContent = new MultipartFormDataContent();
        AddBpmnFile(followUpContent, ReadAsset("camunda-order-process.bpmn"), "file");
        var followUpResponse = await PostAuthenticatedAsync("bpmn/analyze", followUpContent, "workflows/definitions:view");
        await Assert.That(followUpResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Import_OfADocumentWithASubprocessReusingItsParentTopLevelProcessesOwnId_ReturnsUnprocessableEntityAndTheServerStaysAlive()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("subprocess-reuses-parent-process-id.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ImportDuplicateElementId);
        await Assert.That(body).Contains("P", StringComparison.CurrentCulture);

        // elsa-core#8074: a top-level process's own id was never in the pool checked for uniqueness (only its
        // elements were), so a subprocess declared directly inside it that reuses that same id went undetected and
        // overflowed the stack the same way a subprocess nested inside another subprocess does. See the equivalent
        // nested-subprocess test's remarks: reaching the assertions above already proves the process survived; a
        // further successful request proves the host is still serving requests, too.
        using var followUpContent = new MultipartFormDataContent();
        AddBpmnFile(followUpContent, ReadAsset("camunda-order-process.bpmn"), "file");
        var followUpResponse = await PostAuthenticatedAsync("bpmn/analyze", followUpContent, "workflows/definitions:view");
        await Assert.That(followUpResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Import_OfADocumentWithTwoTopLevelProcessesSharingAnId_ReturnsUnprocessableEntity()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("two-process-duplicate-id.bpmn"), "file");
        content.Add(new StringContent("shared"), "ProcessId");

        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ImportDuplicateElementId);
        await Assert.That(body).Contains("shared", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Import_OfAValidDocument_ReturnsOkAndPersistsADefinition()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");

        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        await Assert.That(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("definitionId").GetString())).IsFalse();
    }

    [Test]
    public async Task Export_OfAMissingDefinition_ReturnsNotFound()
    {
        var response = await GetAuthenticatedAsync("bpmn/definitions/does-not-exist/export", "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Export_OfADefinitionNeverImportedFromBpmn_ReturnsUnprocessableEntity()
    {
        var definitionId = await CreateNonBpmnDefinitionAsync();

        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ExportNotImported);
        await Assert.That(body).Contains("does not currently carry BPMN source", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Export_OfADefinitionWithAChangedGraphAfterADesignerSave_ReturnsUnprocessableEntityCodedAsStale()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        await SaveDraftFromTheDesignerAsync(definitionId);

        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ExportSourceStale);
        await Assert.That(body).Contains("has changed since it was imported", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Export_WithAMalformedVersion_ReturnsBadRequest()
    {
        var response = await GetAuthenticatedAsync("bpmn/definitions/does-not-exist/export?VersionOptions=not-a-number", "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("not-a-number", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Export_OfAFreshlyImportedDefinition_ReturnsOkWithBpmnXml()
    {
        using var importContent = new MultipartFormDataContent();
        AddBpmnFile(importContent, ReadAsset("camunda-order-process.bpmn"), "file");
        var importResponse = await PostAuthenticatedAsync("bpmn/import", importContent, "workflows/definitions:write");
        await Assert.That(importResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var importDocument = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        var definitionId = importDocument.RootElement.GetProperty("definitionId").GetString();

        var exportResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "workflows/definitions:view");

        await Assert.That(exportResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var exportedXml = await exportResponse.Content.ReadAsStringAsync();
        await Assert.That(exportedXml).Contains("order-process", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Export_WhenUnauthenticated_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "bpmn/definitions/does-not-exist/export");
        var response = await HttpClient.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Import_WhenAuthenticatedWithoutTheRequiredPermission_ReturnsForbidden()
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("camunda-order-process.bpmn"), "file");

        // Authenticated, but only holds the read permission Export needs, not the write permission Import needs.
        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DocumentGet_OfAMissingDefinition_ReturnsNotFound()
    {
        var response = await GetAuthenticatedAsync("bpmn/definitions/does-not-exist/document", "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DocumentGet_OfADefinitionNeverImportedFromBpmn_ReturnsUnprocessableEntity()
    {
        var definitionId = await CreateNonBpmnDefinitionAsync();

        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ExportNotImported);
        await Assert.That(body).Contains("does not currently carry BPMN source", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task DocumentGet_OfADefinitionWithAChangedGraphAfterADesignerSave_ReturnsUnprocessableEntityCodedAsStale()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        await SaveDraftFromTheDesignerAsync(definitionId);

        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ExportSourceStale);
        await Assert.That(body).Contains("has changed since it was imported", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task DocumentGet_OfAFreshlyImportedDefinition_ReturnsOkWithTheLibraryFormatDocument()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();

        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        await Assert.That(document.RootElement.GetProperty("processes").EnumerateArray().Select(process => process.GetProperty("processId").GetString())).Contains("order-process");
    }

    [Test]
    public async Task DocumentGet_WhenAuthenticatedWithoutTheRequiredPermission_ReturnsForbidden()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();

        // Authenticated, but only holds the write permission Put needs, not the read permission Get needs.
        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:write");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DocumentPut_OfAMissingDefinition_ReturnsNotFound()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await PutAuthenticatedAsync("bpmn/definitions/does-not-exist/document", content, null, "workflows/definitions:write");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DocumentPut_WithoutIfMatch_ReturnsPreconditionRequired()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", content, null, "workflows/definitions:write");

        await Assert.That(response.StatusCode).IsEqualTo((HttpStatusCode)428);
        await Assert.That(CodeOf(await response.Content.ReadAsStringAsync())).IsEqualTo(BpmnErrorCodes.DocumentPreconditionRequired);
        await Assert.That(await LatestVersionOfAsync(definitionId)).IsEqualTo(versionBeforePut);
    }

    [Test]
    public async Task DocumentPut_WithAWildcardIfMatch_ReturnsPreconditionRequiredAndOverwritesNothing()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var (_, documentJson) = await GetDocumentAsync(definitionId);
        var storedBeforePut = await LatestStoredAsync(definitionId);

        // "*" matches whatever is currently stored, so honouring it would be exactly the blind overwrite the required
        // If-Match exists to prevent.
        var response = await PutDocumentAsync(definitionId, WithFirstShapeMoved(documentJson), "*");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.PreconditionRequired);
        await Assert.That(CodeOf(await response.Content.ReadAsStringAsync())).IsEqualTo(BpmnErrorCodes.DocumentPreconditionRequired);
        await Assert.That(await LatestStoredAsync(definitionId)).IsEqualTo(storedBeforePut);
    }

    [Test]
    public async Task DocumentPut_WithTheCurrentIfMatch_ReturnsOkWithANewETagWhenTheContentChanged()
    {
        var definitionId = await ImportWrittenBackAsync("camunda-order-process.bpmn");
        var (currentETag, documentJson) = await GetDocumentAsync(definitionId);

        var putResponse = await PutDocumentAsync(definitionId, WithFirstShapeMoved(documentJson), currentETag);

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var newETag = ETagOf(putResponse);
        await Assert.That(newETag).IsNotNull();
        await Assert.That(newETag).IsNotEqualTo(currentETag);
        // The returned ETag names what was stored, so it is exactly what the next GET hands out.
        await Assert.That((await GetDocumentAsync(definitionId)).ETag).IsEqualTo(newETag);
    }

    [Test]
    [Arguments("camunda-order-process.bpmn")]
    [Arguments("subprocess-boundary-events.bpmn")]
    [Arguments("transaction-compensation.bpmn")]
    [Arguments("nested-subprocesses.bpmn")]
    [Arguments("top-level-call-activity.bpmn")]
    public async Task DocumentPut_OfUnchangedContent_ReturnsTheETagTheGetReturned(string assetFileName)
    {
        // The nested fixtures prove the subprocess bodies a PUT writes back from the stored document come out
        // byte-identical every time, including a multi-instance subprocess, top-level or nested, whose marker the library
        // also retains in the body and would otherwise write one more copy of on every PUT.
        var definitionId = await ImportWrittenBackAsync(assetFileName);
        var (currentETag, documentJson) = await GetDocumentAsync(definitionId);

        var putResponse = await PutDocumentAsync(definitionId, documentJson, currentETag);

        // Content-addressed: writing back exactly what is stored overwrites nothing, so the validator stays the same.
        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(ETagOf(putResponse)).IsEqualTo(currentETag);
    }

    [Test]
    public async Task DocumentPut_AfterAnInterveningDocumentPut_ReturnsPreconditionFailedAndOverwritesNothing()
    {
        var definitionId = await ImportWrittenBackAsync("camunda-order-process.bpmn");
        var (staleETag, documentJson) = await GetDocumentAsync(definitionId);
        var storedBeforeInterveningPut = await LatestStoredAsync(definitionId);

        var interveningPut = await PutDocumentAsync(definitionId, WithFirstShapeMoved(documentJson), staleETag);
        await Assert.That(interveningPut.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // A layout-only edit to an unpublished draft: same row, same version, same activity graph — only the stored
        // document moved, so this is the case that proves the document itself is part of the ETag.
        var storedAfterInterveningPut = await LatestStoredAsync(definitionId);
        await Assert.That(storedAfterInterveningPut).IsEqualTo(storedBeforeInterveningPut with { SourceXml = storedAfterInterveningPut.SourceXml });
        await Assert.That(storedAfterInterveningPut.SourceXml).IsNotEqualTo(storedBeforeInterveningPut.SourceXml);

        await AssertStalePutIsRefusedAsync(definitionId, documentJson, staleETag);
    }

    [Test]
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
        await Assert.That(importResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Imported into the unpublished draft in place: same row, same version.
        var storedAfterImport = await LatestStoredAsync(definitionId);
        await Assert.That((storedAfterImport.Id, storedAfterImport.Version)).IsEqualTo((storedBeforeImport.Id, storedBeforeImport.Version));

        await AssertStalePutIsRefusedAsync(definitionId, documentJson, staleETag);
    }

    [Test]
    public async Task DocumentPut_AfterAnInterveningDesignerSaveOfTheDraft_ReturnsPreconditionFailedAndOverwritesNothing()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var (staleETag, documentJson) = await GetDocumentAsync(definitionId);
        var storedBeforeSave = await LatestStoredAsync(definitionId);

        await SaveDraftFromTheDesignerAsync(definitionId);

        // Saved in place with every custom property carried forward: same row, same version, same stored document —
        // only the activity graph moved, so this is the case that proves the graph itself is part of the ETag.
        var storedAfterSave = await LatestStoredAsync(definitionId);
        await Assert.That(storedAfterSave).IsEqualTo(storedBeforeSave with { StringData = storedAfterSave.StringData });
        await Assert.That(storedAfterSave.StringData).IsNotEqualTo(storedBeforeSave.StringData);

        await AssertStalePutIsRefusedAsync(definitionId, documentJson, staleETag);
    }

    [Test]
    public async Task DocumentPut_WithMalformedJson_ReturnsBadRequestAndPersistsNoNewDraft()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var content = new StringContent("{ not valid json", Encoding.UTF8, "application/json");
        var response = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", content, ETagOf(getResponse), "workflows/definitions:write");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(await LatestVersionOfAsync(definitionId)).IsEqualTo(versionBeforePut);
    }

    [Test]
    public async Task DocumentPut_ThatRemovesARequiredActivityBinding_ReturnsUnprocessableEntityAndPersistsNoNewDraft()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
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

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await putResponse.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ImportBindingInvalid);
        await Assert.That(body).Contains("nothing binds it to an Elsa activity", StringComparison.CurrentCulture);
        await Assert.That(await LatestVersionOfAsync(definitionId)).IsEqualTo(versionBeforePut);
    }

    [Test]
    public async Task DocumentPut_WithARepeatedElementId_ReturnsUnprocessableEntityAndPersistsNoNewDraftAndTheServerStaysAlive()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        var (etag, documentJson) = await GetDocumentAsync(definitionId);
        var putResponse = await PutDocumentAsync(definitionId, WithADuplicatedElementId(documentJson), etag);

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await putResponse.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ImportDuplicateElementId);
        await Assert.That(await LatestVersionOfAsync(definitionId)).IsEqualTo(versionBeforePut);

        // See the equivalent Import test's remarks: reaching the assertions above already proves the process
        // survived reading this document; a further successful request proves the host is still serving requests.
        await Assert.That((await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view")).StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DocumentPut_WithATopLevelProcessIdReusingAnExistingSubprocessId_ReturnsUnprocessableEntityAndPersistsNoNewDraftAndTheServerStaysAlive()
    {
        // nested-subprocesses.bpmn already declares a subprocess with id "Outer"; renaming the top-level process's
        // own id to "Outer" reproduces elsa-core#8074's collision through the document PUT, where the nested scope
        // ("Outer"'s stored body) comes not from the edited document but from the definition's already-stored
        // source (see ImportDocumentAsync's remarks on storedNestedScopes).
        var definitionId = await ImportWrittenBackAsync("nested-subprocesses.bpmn");
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        var (etag, documentJson) = await GetDocumentAsync(definitionId);
        var putResponse = await PutDocumentAsync(definitionId, WithTopLevelProcessIdReusingASubprocessId(documentJson, "Outer"), etag);

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
        var body = await putResponse.Content.ReadAsStringAsync();
        await Assert.That(CodeOf(body)).IsEqualTo(BpmnErrorCodes.ImportDuplicateElementId);
        await Assert.That(body).Contains("Outer", StringComparison.CurrentCulture);
        await Assert.That(await LatestVersionOfAsync(definitionId)).IsEqualTo(versionBeforePut);

        // See the equivalent repeated-element-id test's remarks: reaching the assertions above already proves the
        // process survived reading this document; a further successful request proves the host is still serving
        // requests, too.
        await Assert.That((await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view")).StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DocumentPut_UnchangedDocument_ReturnsOkAndTheSameFindingsAsImport()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var documentJson = await getResponse.Content.ReadAsStringAsync();

        using var putContent = new StringContent(documentJson, Encoding.UTF8, "application/json");
        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, ETagOf(getResponse), "workflows/definitions:write");

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var putResult = JsonDocument.Parse(await putResponse.Content.ReadAsStringAsync());
        await Assert.That(putResult.RootElement.GetProperty("definitionId").GetString()).IsEqualTo(definitionId);
        await Assert.That(putResult.RootElement.GetProperty("analysis").GetProperty("processIds").EnumerateArray().Select(processId => processId.GetString())).Contains("order-process");

        var exportResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "workflows/definitions:view");
        await Assert.That(exportResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var exportedXml = await exportResponse.Content.ReadAsStringAsync();
        await Assert.That(exportedXml).Contains("order-process", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task DocumentPut_WithABindingChange_PreservesTheDefinitionsNonBpmnMetadata()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        await SetNonBpmnMetadataOnDraftAsync(definitionId);
        var metadataBeforePut = await CaptureMetadataAsync(definitionId);

        var (etag, documentJson) = await GetDocumentAsync(definitionId);
        var putResponse = await PutDocumentAsync(definitionId, WithNotifyWarehouseTextChanged(documentJson), etag);

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Every field SetNonBpmnMetadataOnDraftAsync set survived the PUT unchanged.
        var metadataAfterPut = await CaptureMetadataAsync(definitionId);
        await Assert.That(metadataAfterPut).IsEqualTo(metadataBeforePut);

        // ...while the graph reflects the binding change the PUT carried.
        var exportResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/export", "workflows/definitions:view");
        await Assert.That(exportResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var exportedXml = await exportResponse.Content.ReadAsStringAsync();
        await Assert.That(exportedXml).Contains("Notifying the warehouse, rebound via document PUT", StringComparison.CurrentCulture);
    }

    [Test]
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
        await Assert.That(importResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var metadataAfterImport = await CaptureMetadataAsync(definitionId);
        await Assert.That(metadataAfterImport).IsNotEqualTo(metadataBeforeImport);
        await Assert.That(metadataAfterImport.Name).IsEqualTo("Order Process");
        await Assert.That(metadataAfterImport.Description).IsNull();
        await Assert.That(metadataAfterImport.VariableSummary).IsEqualTo(string.Empty);
        await Assert.That(metadataAfterImport.UsableAsActivity).IsNull();
        await Assert.That(metadataAfterImport.InputSummary).IsEqualTo(string.Empty);
        await Assert.That(metadataAfterImport.OutputSummary).IsEqualTo(string.Empty);
        await Assert.That(metadataAfterImport.OutcomeSummary).IsEqualTo(string.Empty);
        await Assert.That(metadataAfterImport.ToolVersion).IsNull();
        await Assert.That(metadataAfterImport.IsReadonly).IsFalse();
        await Assert.That(metadataAfterImport.CustomPropertyValue).IsNull();
    }

    [Test]
    public async Task DocumentPut_AfterAMetadataOnlySaveCreatesANewDraftFromAPublishedVersion_SucceedsAndRecordsAFreshMarker()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        await MarkLatestPublishedAsync(definitionId);
        await RenameLatestDraftThroughTheDesignerAsync(definitionId, "Renamed through the designer");

        // A metadata-only save through the designer path bumps a published definition to a new draft (N+1) without
        // touching the graph, which is exactly the shape this issue is about: the document GET must not refuse that
        // draft as stale, so the PUT that follows (W11's flow) has an ETag to send at all.
        await Assert.That(await LatestVersionOfAsync(definitionId)).IsEqualTo(2);
        var (etag, documentJson) = await GetDocumentAsync(definitionId);

        var putResponse = await PutDocumentAsync(definitionId, WithFirstShapeMoved(documentJson), etag);

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var stored = await FindLatestDefinitionAsync(definitionId);
        await Assert.That(stored.CustomProperties.TryGetValue<int>(BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey, out var sourceVersion)).IsTrue();
        await Assert.That(sourceVersion).IsEqualTo(stored.Version);
        await Assert.That(stored.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceGraphHashCustomPropertyKey, out var sourceGraphHash)).IsTrue();
        await Assert.That(string.IsNullOrEmpty(sourceGraphHash)).IsFalse();
    }

    [Test]
    public async Task DocumentPut_WhenAuthenticatedWithoutTheRequiredPermission_ReturnsForbidden()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Authenticated, but only holds the read permission Get needs, not the write permission Put needs.
        var response = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", content, null, "workflows/definitions:view");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DocumentPut_ForASingleProcessDefinitionImportedBeforeSourceProcessIdExisted_ReturnsOk()
    {
        var definitionId = await ImportCamundaOrderProcessAsync();
        // Simulates a definition imported before ImportAsync started recording SourceProcessIdCustomPropertyKey.
        await RemoveSourceProcessIdCustomPropertyAsync(definitionId);

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var documentJson = await getResponse.Content.ReadAsStringAsync();

        using var putContent = new StringContent(documentJson, Encoding.UTF8, "application/json");
        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, ETagOf(getResponse), "workflows/definitions:write");

        // A single-process document does not need SourceProcessId to disambiguate anything, so the missing
        // property does not stop the edit from succeeding.
        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DocumentPut_ForATwoProcessDefinitionImportedBeforeSourceProcessIdExisted_ReturnsBadRequestAndPersistsNoNewDraft()
    {
        var definitionId = await ImportTwoProcessDocumentAsync("first-process");
        // Simulates a definition imported before ImportAsync started recording SourceProcessIdCustomPropertyKey:
        // without it, Put has no way to know which of the document's two processes to re-bind.
        await RemoveSourceProcessIdCustomPropertyAsync(definitionId);
        var versionBeforePut = await LatestVersionOfAsync(definitionId);

        var getResponse = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var documentJson = await getResponse.Content.ReadAsStringAsync();

        using var putContent = new StringContent(documentJson, Encoding.UTF8, "application/json");
        var putResponse = await PutAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", putContent, ETagOf(getResponse), "workflows/definitions:write");

        await Assert.That((int)putResponse.StatusCode is >= 400 and < 500).IsTrue().Because($"Expected a 4xx status code, got {(int)putResponse.StatusCode}.");
        var body = await putResponse.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("specify which one to import", StringComparison.CurrentCulture);
        await Assert.That(await LatestVersionOfAsync(definitionId)).IsEqualTo(versionBeforePut);
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
        using var scope = Services.CreateScope();
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

    /// <summary>The <c>code</c> field of a coded BPMN error response body (see <see cref="BpmnErrorCodes"/>), or <c>null</c> if it carries none.</summary>
    private static string? CodeOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary>GETs the document of <paramref name="definitionId"/>, asserting it succeeded, and returns its ETag and body.</summary>
    private async Task<(string? ETag, string Json)> GetDocumentAsync(string definitionId)
    {
        var response = await GetAuthenticatedAsync($"bpmn/definitions/{definitionId}/document", "workflows/definitions:view");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.PreconditionFailed);
        await Assert.That(CodeOf(await response.Content.ReadAsStringAsync())).IsEqualTo(BpmnErrorCodes.DocumentPreconditionFailed);
        await Assert.That(await LatestStoredAsync(definitionId)).IsEqualTo(storedBeforePut);
    }

    /// <summary>Moves the document's first diagram shape to the right — a layout-only edit, the kind W14 makes.</summary>
    private static string WithFirstShapeMoved(string documentJson)
    {
        var document = JsonNode.Parse(documentJson)!;
        var bounds = document["diagrams"]![0]!["plane"]!["shapes"]![0]!["bounds"]!;
        bounds["x"] = bounds["x"]!.GetValue<double>() + 10;
        return document.ToJsonString();
    }

    /// <summary>Renames the document's last top-level element to its first element's id, so the two collide (elsa-core#8074).</summary>
    private static string WithADuplicatedElementId(string documentJson)
    {
        var document = JsonNode.Parse(documentJson)!;
        var elements = document["processes"]![0]!["elements"]!.AsArray();
        var firstElementId = elements[0]!["elementId"]!.GetValue<string>();
        elements[^1]!["elementId"] = firstElementId;
        return document.ToJsonString();
    }

    /// <summary>Renames the document's top-level process id to <paramref name="subprocessId"/>, so it collides with a subprocess that already declares that id (elsa-core#8074).</summary>
    private static string WithTopLevelProcessIdReusingASubprocessId(string documentJson, string subprocessId)
    {
        var document = JsonNode.Parse(documentJson)!;
        document["processes"]![0]!["processId"] = subprocessId;
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
        using var scope = Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        var draft = await publisher.GetDraftAsync(definitionId, VersionOptions.Latest);
        await Assert.That(draft).IsNotNull();

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
        using var scope = Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await store.FindAsync(filter);
        await Assert.That(definition).IsNotNull();
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
        using var scope = Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        var serializer = scope.ServiceProvider.GetRequiredService<IActivitySerializer>();
        var draft = await publisher.GetDraftAsync(definitionId, VersionOptions.Latest);
        await Assert.That(draft).IsNotNull();

        var deserializedRoot = serializer.Deserialize(draft!.StringData!);
        await Assert.That(deserializedRoot).IsOfType(typeof(BpmnProcess));
        var root = (BpmnProcess)deserializedRoot;
        var writeLine = (await Assert.That(root.Activities.OfType<WriteLine>()).HasSingleItem())!;
        writeLine.Text = new("Notifying the warehouse, edited in the designer");
        draft.StringData = serializer.Serialize(root);

        await publisher.SaveDraftAsync(draft);
    }

    /// <summary>Imports <c>camunda-order-process.bpmn</c> through the real endpoint and returns the resulting <c>definitionId</c>.</summary>
    private Task<string> ImportCamundaOrderProcessAsync() => ImportAssetAsync("camunda-order-process.bpmn");

    /// <summary>Imports the fixture <paramref name="assetFileName"/> through the real endpoint and returns the resulting <c>definitionId</c>.</summary>
    private async Task<string> ImportAssetAsync(string assetFileName)
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset(assetFileName), "file");
        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("definitionId").GetString()!;
    }

    /// <summary>
    /// Imports the fixture <paramref name="assetFileName"/> and writes its document straight back once through the
    /// document PUT, so what is stored is the writer's own rendering of it rather than the uploaded bytes. From there only
    /// an actual edit changes the stored content, which is what lets a test attribute an ETag change — or its absence — to
    /// one write.
    /// </summary>
    private async Task<string> ImportWrittenBackAsync(string assetFileName)
    {
        var definitionId = await ImportAssetAsync(assetFileName);
        var (etag, documentJson) = await GetDocumentAsync(definitionId);
        var response = await PutDocumentAsync(definitionId, documentJson, etag);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        return definitionId;
    }

    /// <summary>Imports <c>two-process.bpmn</c>, picking <paramref name="processId"/>, and returns the resulting <c>definitionId</c>.</summary>
    private async Task<string> ImportTwoProcessDocumentAsync(string processId)
    {
        using var content = new MultipartFormDataContent();
        AddBpmnFile(content, ReadAsset("two-process.bpmn"), "file");
        content.Add(new StringContent(processId), "ProcessId");
        var response = await PostAuthenticatedAsync("bpmn/import", content, "workflows/definitions:write");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("definitionId").GetString()!;
    }

    /// <summary>
    /// Removes <see cref="BpmnInterchangeDocumentService.SourceProcessIdCustomPropertyKey"/> from the latest version of
    /// <paramref name="definitionId"/>, simulating a definition imported before <c>ImportAsync</c> started recording it.
    /// </summary>
    private async Task RemoveSourceProcessIdCustomPropertyAsync(string definitionId)
    {
        using var scope = Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await store.FindAsync(filter);
        await Assert.That(definition).IsNotNull();
        definition!.CustomProperties.Remove(BpmnInterchangeDocumentService.SourceProcessIdCustomPropertyKey);
        await store.SaveAsync(definition);
    }

    /// <summary>
    /// Publishes the latest version of <paramref name="definitionId"/> through the real
    /// <see cref="IWorkflowDefinitionPublisher"/>. See <see cref="DefinitionPublishing.PublishLatestAsync"/>.
    /// </summary>
    private async Task MarkLatestPublishedAsync(string definitionId)
    {
        using var scope = Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        await DefinitionPublishing.PublishLatestAsync(publisher, definitionId);
    }

    /// <summary>
    /// Renames the latest draft of <paramref name="definitionId"/> the way the workflow-definition save endpoint
    /// Studio's designer calls does — <see cref="IWorkflowDefinitionPublisher.GetDraftAsync"/> then
    /// <see cref="IWorkflowDefinitionPublisher.SaveDraftAsync"/> — without touching the activity graph: a
    /// metadata-only save, which carries a published definition to a new draft version without moving the graph the
    /// stored BPMN source describes.
    /// </summary>
    private async Task RenameLatestDraftThroughTheDesignerAsync(string definitionId, string name)
    {
        using var scope = Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        var draft = await publisher.GetDraftAsync(definitionId, VersionOptions.Latest);
        await Assert.That(draft).IsNotNull();
        draft!.Name = name;
        await publisher.SaveDraftAsync(draft);
    }

    private async Task<int> LatestVersionOfAsync(string definitionId) => (await LatestStoredAsync(definitionId)).Version;

    /// <summary>
    /// A snapshot of the latest version of <paramref name="definitionId"/>: its id, version, activity graph and stored BPMN
    /// document — everything a document PUT rewrites that the document ETag covers.
    /// </summary>
    private async Task<StoredDefinition> LatestStoredAsync(string definitionId)
    {
        using var scope = Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var definition = await store.FindAsync(filter);
        await Assert.That(definition).IsNotNull();
        definition!.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey, out var sourceXml);
        return new(definition.Id, definition.Version, definition.StringData, sourceXml);
    }

    private sealed record StoredDefinition(string Id, int Version, string? StringData, string? SourceXml);

}
