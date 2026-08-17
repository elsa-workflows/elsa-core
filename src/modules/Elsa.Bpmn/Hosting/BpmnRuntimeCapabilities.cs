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
/// <para>
/// Capability refusal — <c>BpmnGraph.Build</c>'s <c>ThrowIfUnmet</c>, and the mirroring check in
/// <c>BpmnInterchangeDocumentService.EnsureCapabilitiesSatisfied</c> — is currently unreachable: the pinned
/// <c>Bpmn.Semantics</c> version defines no flag <see cref="Declared"/> does not already cover, so nothing can be
/// refused for a missing capability today. <c>Elsa.Bpmn.UnitTests.BpmnRuntimeCapabilitiesTests</c> is what will say
/// when that stops being true — it fails the moment the library adds a flag this constant doesn't declare, and its
/// failure message names the decision that needs making.
/// </para>
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
