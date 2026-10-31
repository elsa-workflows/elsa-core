namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>What a successful XML <c>PUT</c>, or the creation of a BPMN workflow, reported.</summary>
/// <param name="Import">The definition's identity and the findings its import produced.</param>
/// <param name="ETag">The stored revision's <c>ETag</c>, or <see langword="null"/> when the response did not carry one.</param>
/// <param name="DroppedForeignContent">
/// The foreign content the previously stored document had that the saved one does not. The save succeeded regardless;
/// a creation drops nothing.
/// </param>
public sealed record BpmnXmlSaveResult(BpmnImportResultModel Import, string? ETag, IReadOnlyList<BpmnForeignContentModel> DroppedForeignContent);
