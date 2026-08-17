using Bpmn.Semantics;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// The single source of truth for the BPMN host capabilities this runtime module declares it can honour.
/// </summary>
/// <remarks>
/// <see cref="BpmnScopeHost"/> is <c>internal</c> — <c>Elsa.Bpmn</c> is the runtime module, kept free of the
/// interchange package's dependency closure per D12 — so its <see cref="BpmnScopeHost.Capabilities"/> constant
/// cannot be read from outside this assembly. This type exists solely to make that same value reachable as public
/// API, so <c>Elsa.Bpmn.Interchange</c> — which already references this module, so making the constant reachable
/// does not touch the D12 split — can refuse a document at import time for exactly the capabilities the runtime
/// would refuse it for at first execution, rather than restating the set as a second literal that can silently drift
/// from this one. <see cref="BpmnScopeHost.Capabilities"/> is defined in terms of <see cref="Declared"/>, not the
/// other way around, so there remains exactly one place this set is spelled out.
/// </remarks>
public static class BpmnRuntimeCapabilities
{
    /// <summary>
    /// What <see cref="BpmnScopeHost"/> promises it can do. See its own remarks for what each flag is honoured by
    /// and why this is spelled out rather than written as <see cref="BpmnHostCapabilities.Full"/>.
    /// </summary>
    public const BpmnHostCapabilities Declared =
        BpmnHostCapabilities.SubtreeCancellation | BpmnHostCapabilities.ScopeSignalling | BpmnHostCapabilities.IterationScopes | BpmnHostCapabilities.ScopeVariables;
}
