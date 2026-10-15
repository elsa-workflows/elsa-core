namespace Elsa.Bpmn.Hosting;

/// <summary>
/// The high-water mark of diagnostics a scope has already projected onto its execution log: the numeric ordinal
/// of the last <c>diag:N</c> id it turned into a journal entry.
/// </summary>
/// <remarks>
/// Persisted under <see cref="BpmnScopeMemory.DiagnosticsCursorPropertyKey"/>, next to
/// <see cref="BpmnScopeMemory.ExecutionStatePropertyKey"/> and <see cref="BpmnScopeMemory.WorkLedgerPropertyKey"/>,
/// and serialized the same way. Without it, a scope resumed from persisted state would re-evaluate the same
/// surviving diagnostics <c>Bpmn.Model.State.BpmnExecutionState.Prune</c> carried forward and project every one of
/// them a second time.
/// </remarks>
/// <param name="LastSequence">The ordinal of the last diagnostic id (<c>diag:N</c>) already projected.</param>
internal sealed record BpmnDiagnosticsCursor(int LastSequence);
