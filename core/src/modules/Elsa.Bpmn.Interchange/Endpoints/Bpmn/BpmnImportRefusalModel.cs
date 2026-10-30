using Bpmn.Semantics;
using Elsa.Bpmn.Interchange.Exceptions;
using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>Why an import of a document would be refused, as reported over the API.</summary>
[PublicAPI]
public sealed class BpmnImportRefusalModel
{
    /// <summary>The error code an import responds with, such as <see cref="BpmnErrorCodes.ImportBindingInvalid"/>.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>The message an import responds with.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>The BPMN ids of the elements the refusal is about; empty when it names no element.</summary>
    public IReadOnlyCollection<string> ElementIds { get; init; } = [];

    /// <summary>The refusal <paramref name="exception"/> stands for, or null when it is not one an import refuses a document with.</summary>
    internal static BpmnImportRefusalModel? For(Exception exception) => exception switch
    {
        BpmnDuplicateElementIdException duplicate => new()
        {
            Code = BpmnErrorCodes.ImportDuplicateElementId,
            Message = duplicate.Message,
            ElementIds = duplicate.DuplicateElementIds.ToList()
        },
        BpmnCapabilityException capability => new()
        {
            Code = BpmnErrorCodes.ImportCapabilityUnsupported,
            Message = BpmnCapabilityErrorFormatter.Format(capability),
            ElementIds = capability.DrivingElementIds.ToList()
        },
        BpmnConditionRefusedException condition => new()
        {
            Code = condition.Code,
            Message = condition.Message,
            ElementIds = condition.FlowIds.ToList()
        },
        BpmnBindingException binding => new()
        {
            Code = BpmnErrorCodes.ImportBindingInvalid,
            Message = binding.Message,
            ElementIds = binding.ElementId is { } elementId ? [elementId] : []
        },
        BpmnProcessInvalidException process => new()
        {
            Code = BpmnErrorCodes.ImportProcessInvalid,
            Message = process.Message
        },
        _ => null
    };
}
