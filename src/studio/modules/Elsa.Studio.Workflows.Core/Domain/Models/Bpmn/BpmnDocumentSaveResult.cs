namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// What a successful document <c>PUT</c> reported: the re-imported definition's identity and analysis, in the same
/// shape <c>bpmn/import</c> returns, and the <c>ETag</c> of the draft the <c>PUT</c> stored.
/// </summary>
/// <param name="Import">The re-imported definition's identity and the findings the read produced.</param>
/// <param name="ETag">The new revision's <c>ETag</c>, or <see langword="null"/> when the response did not carry one.</param>
public sealed record BpmnDocumentSaveResult(BpmnImportResultModel Import, string? ETag);
