using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Interchange.Endpoints.Bpmn;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using Microsoft.AspNetCore.Http;

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
    [Fact(DisplayName = "A capability refusal is coded bpmn.import.capability-unsupported, carrying the missing capability names and driving element ids as data")]
    public void CapabilityResponseFor_CarriesTheCodeAndTheStructuredData()
    {
        var definition = MultiInstanceDefinition("main", "each");
        var exception = Assert.Throws<BpmnCapabilityException>(() =>
            BpmnInterchangeDocumentService.EnsureCapabilitiesSatisfied(definition, [], BpmnHostCapabilities.None));

        var response = BpmnImportErrorResponses.CapabilityResponseFor(exception);

        Assert.Equal(BpmnErrorCodes.ImportCapabilityUnsupported, response.Code);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);

        var message = Assert.Single(response.Errors["generalErrors"]);
        Assert.Contains("IterationScopes", message);
        Assert.Contains("each", message);

        dynamic data = response.Data!;
        Assert.Equal(new[] { "IterationScopes" }, (IReadOnlyList<string>)data.Capabilities);
        Assert.Equal(new[] { "each" }, (IReadOnlyList<string>)data.ElementIds);
    }

    [Fact(DisplayName = "A binding refusal is coded bpmn.import.binding-invalid, with no structured data, keeping the exception's own message")]
    public void BindingInvalidResponseFor_CarriesTheCodeAndTheOriginalMessage()
    {
        var exception = new BpmnBindingException("BPMN element 'task-1' declares no binding.");

        var response = BpmnImportErrorResponses.BindingInvalidResponseFor(exception);

        Assert.Equal(BpmnErrorCodes.ImportBindingInvalid, response.Code);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.Equal(exception.Message, Assert.Single(response.Errors["generalErrors"]));
        Assert.Null(response.Data);
    }

    [Fact(DisplayName = "A duplicate-element-id refusal is coded bpmn.import.duplicate-element-id, carrying the duplicated ids as data")]
    public void DuplicateElementIdResponseFor_CarriesTheCodeAndTheStructuredData()
    {
        var exception = new BpmnDuplicateElementIdException("The document declares the same element id more than once: Outer.", ["Outer"]);

        var response = BpmnImportErrorResponses.DuplicateElementIdResponseFor(exception);

        Assert.Equal(BpmnErrorCodes.ImportDuplicateElementId, response.Code);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.Equal(exception.Message, Assert.Single(response.Errors["generalErrors"]));

        dynamic data = response.Data!;
        Assert.Equal(new[] { "Outer" }, (IReadOnlyList<string>)data.ElementIds);
    }

    [Fact(DisplayName = "A definition-not-found refusal is coded bpmn.document.not-found")]
    public void NotFoundResponseFor_CarriesTheCode()
    {
        var exception = new BpmnDefinitionNotFoundException("Workflow definition 'def-1' does not exist, so its BPMN document cannot be edited.");

        var response = BpmnImportErrorResponses.NotFoundResponseFor(exception);

        Assert.Equal(BpmnErrorCodes.DocumentNotFound, response.Code);
        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        Assert.Equal(exception.Message, Assert.Single(response.Errors["generalErrors"]));
        Assert.Null(response.Data);
    }

    [Theory(DisplayName = "Each BpmnExportUnavailableReason maps to its own code")]
    [InlineData(BpmnExportUnavailableReason.NotImported, BpmnErrorCodes.ExportNotImported)]
    [InlineData(BpmnExportUnavailableReason.SourceVersionUnknown, BpmnErrorCodes.ExportSourceVersionUnknown)]
    [InlineData(BpmnExportUnavailableReason.SourceStale, BpmnErrorCodes.ExportSourceStale)]
    public void ResponseFor_MapsEachReasonToItsOwnCode(BpmnExportUnavailableReason reason, string expectedCode)
    {
        var exception = new BpmnExportUnavailableException("Workflow definition 'def-1' cannot be exported.", reason);

        var response = BpmnExportErrorResponses.ResponseFor(exception);

        Assert.Equal(expectedCode, response.Code);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.Equal(exception.Message, Assert.Single(response.Errors["generalErrors"]));
        Assert.Null(response.Data);
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
