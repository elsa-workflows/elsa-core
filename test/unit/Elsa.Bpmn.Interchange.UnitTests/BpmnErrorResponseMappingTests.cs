using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Elsa.Bpmn.Interchange.UnitTests;

/// <summary>
/// The BPMN endpoints' exception-to-response mapping (<see cref="BpmnImportErrorResponses"/>,
/// <see cref="BpmnExportErrorResponses"/>) turns a thrown exception into a stable <see cref="BpmnErrorCodes"/> code
/// and, for the capability refusal, structured data — exercised here at the mapping itself, the same way
/// <see cref="BpmnInterchangeDocumentServiceCapabilityTests"/> exercises capability refusal directly, rather than
/// through an HTTP round trip: <c>Bpmn.*</c> 0.2.0 declares every capability this deployment's runtime needs, so a
/// document a real import refuses on capability grounds cannot be produced through the public API today. The same is
/// true of <see cref="BpmnDefinitionNotFoundException"/> (only reachable through a race the endpoints' own
/// existence checks close off) and <see cref="BpmnExportUnavailableReason.SourceVersionUnknown"/> (only reachable
/// through custom properties edited outside <c>ImportAsync</c>). See
/// <c>Elsa.Bpmn.Interchange.IntegrationTests.Endpoints.BpmnInterchangeEndpointTests</c> for the codes reachable
/// through a real HTTP request.
/// </summary>
public class BpmnErrorResponseMappingTests
{
    [Test]
    [DisplayName("A capability refusal is coded bpmn.import.capability-unsupported, carrying the missing capability names and driving element ids as data")]
    public async Task CapabilityResponseFor_CarriesTheCodeAndTheStructuredData()
    {
        var definition = MultiInstanceDefinition("main", "each");
        var exception = Assert.ThrowsExactly<BpmnCapabilityException>(() =>
            BpmnInterchangeDocumentService.EnsureCapabilitiesSatisfied(definition, [], BpmnHostCapabilities.None));

        var response = BpmnImportErrorResponses.CapabilityResponseFor(exception);

        await Assert.That(response.Code).IsEqualTo(BpmnErrorCodes.ImportCapabilityUnsupported);
        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status422UnprocessableEntity);

        var message = await Assert.That(response.Errors["generalErrors"]).HasSingleItem();
        await Assert.That(message).Contains("IterationScopes");
        await Assert.That(message).Contains("each");

        dynamic data = response.Data!;
        await Assert.That((IReadOnlyList<string>)data.Capabilities).IsEquivalentTo(new[] { "IterationScopes" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That((IReadOnlyList<string>)data.ElementIds).IsEquivalentTo(new[] { "each" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("A binding refusal is coded bpmn.import.binding-invalid, with no structured data, keeping the exception's own message")]
    public async Task BindingInvalidResponseFor_CarriesTheCodeAndTheOriginalMessage()
    {
        var exception = new BpmnBindingException("BPMN element 'task-1' declares no binding.");

        var response = BpmnImportErrorResponses.BindingInvalidResponseFor(exception);

        await Assert.That(response.Code).IsEqualTo(BpmnErrorCodes.ImportBindingInvalid);
        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status422UnprocessableEntity);
        var generalError = await Assert.That(response.Errors["generalErrors"]).HasSingleItem();
        await Assert.That(generalError).IsEqualTo(exception.Message);
        await Assert.That(response.Data).IsNull();
    }

    [Test]
    [DisplayName("A duplicate-element-id refusal is coded bpmn.import.duplicate-element-id, carrying the duplicated ids as data")]
    public async Task DuplicateElementIdResponseFor_CarriesTheCodeAndTheStructuredData()
    {
        var exception = new BpmnDuplicateElementIdException("The document declares the same element id more than once: Outer.", ["Outer"]);

        var response = BpmnImportErrorResponses.DuplicateElementIdResponseFor(exception);

        await Assert.That(response.Code).IsEqualTo(BpmnErrorCodes.ImportDuplicateElementId);
        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status422UnprocessableEntity);
        var generalError = await Assert.That(response.Errors["generalErrors"]).HasSingleItem();
        await Assert.That(generalError).IsEqualTo(exception.Message);

        dynamic data = response.Data!;
        await Assert.That((IReadOnlyList<string>)data.ElementIds).IsEquivalentTo(new[] { "Outer" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("A definition-not-found refusal is coded bpmn.document.not-found")]
    public async Task NotFoundResponseFor_CarriesTheCode()
    {
        var exception = new BpmnDefinitionNotFoundException("Workflow definition 'def-1' does not exist, so its BPMN document cannot be edited.");

        var response = BpmnImportErrorResponses.NotFoundResponseFor(exception);

        await Assert.That(response.Code).IsEqualTo(BpmnErrorCodes.DocumentNotFound);
        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status404NotFound);
        var generalError = await Assert.That(response.Errors["generalErrors"]).HasSingleItem();
        await Assert.That(generalError).IsEqualTo(exception.Message);
        await Assert.That(response.Data).IsNull();
    }

    [Test]
    [DisplayName("A lost compare-and-swap is coded bpmn.document.precondition-failed, the same 412 If-Match already uses")]
    public async Task PreconditionFailedResponseFor_CarriesTheCode()
    {
        var exception = new BpmnDocumentPreconditionFailedException(
            "The workflow definition has been written since the ETag in If-Match was issued. GET the document again, reapply the edit, and PUT it with the new ETag.");

        var response = BpmnImportErrorResponses.PreconditionFailedResponseFor(exception);

        await Assert.That(response.Code).IsEqualTo(BpmnErrorCodes.DocumentPreconditionFailed);
        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status412PreconditionFailed);
        var generalError = await Assert.That(response.Errors["generalErrors"]).HasSingleItem();
        await Assert.That(generalError).IsEqualTo(exception.Message);
        await Assert.That(response.Data).IsNull();
    }

    [Test]
    [DisplayName("Each BpmnExportUnavailableReason maps to its own code")]
    [Arguments(BpmnExportUnavailableReason.NotImported, BpmnErrorCodes.ExportNotImported)]
    [Arguments(BpmnExportUnavailableReason.SourceVersionUnknown, BpmnErrorCodes.ExportSourceVersionUnknown)]
    [Arguments(BpmnExportUnavailableReason.SourceStale, BpmnErrorCodes.ExportSourceStale)]
    public async Task ResponseFor_MapsEachReasonToItsOwnCode(BpmnExportUnavailableReason reason, string expectedCode)
    {
        var exception = new BpmnExportUnavailableException("Workflow definition 'def-1' cannot be exported.", reason);

        var response = BpmnExportErrorResponses.ResponseFor(exception);

        await Assert.That(response.Code).IsEqualTo(expectedCode);
        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status422UnprocessableEntity);
        var generalError = await Assert.That(response.Errors["generalErrors"]).HasSingleItem();
        await Assert.That(generalError).IsEqualTo(exception.Message);
        await Assert.That(response.Data).IsNull();
    }

    private static BpmnProcessDefinition MultiInstanceDefinition(string processId, string elementId)
    {
        var element = new BpmnElement(
            elementId,
            BpmnElementTypes.ServiceTask,
            bindingRef: $"node-{elementId}",
            loopCharacteristics: new BpmnLoopCharacteristics(isSequential: false, cardinality: 3));

        return new BpmnProcessDefinition(processId, Elements: [element]);
    }
}
