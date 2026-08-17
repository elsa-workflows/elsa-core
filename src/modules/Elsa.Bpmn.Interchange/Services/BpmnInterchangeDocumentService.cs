using System.Text;
using Bpmn.Interchange;
using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;

namespace Elsa.Bpmn.Interchange.Services;

/// <summary>
/// The one code path the Analyze, Import and Export endpoints all sit on top of.
/// </summary>
/// <remarks>
/// <para>
/// <b>Analyze and Import never disagree.</b> Both call <see cref="BpmnXmlReader"/>, which — per its own contract —
/// runs <see cref="BpmnXmlReader.Analyze"/> and <see cref="BpmnXmlReader.Read"/> through the same code, so a preview
/// can never say something the import that follows contradicts.
/// </para>
/// <para>
/// <b>Export carries the whole library-owned document, not a reduced view of it.</b> The only thing <see cref="ImportAsync"/>
/// persists beyond the bound Elsa activity graph is the original BPMN XML text, under <see cref="SourceXmlCustomPropertyKey"/>
/// on the workflow definition's custom properties. <see cref="Export"/> re-reads that same text through the same reader
/// and hands the resulting <see cref="BpmnImportResult"/> — retained extension elements, foreign attributes,
/// unrecognized children and BPMN DI layout included — straight to <see cref="BpmnXmlWriter"/>. Nothing is
/// reconstructed from the Elsa activity tree, which only carries a bindingRef-to-activityId map and would have to
/// throw away everything the reader retained to get there.
/// </para>
/// <para>
/// <b>Capability refusal happens here, at import, not at <c>BpmnGraph.Build</c>.</b> <see cref="BpmnCapabilityRequirements.Analyze"/>
/// is the static half of the same check <c>BpmnGraph.Build</c> performs at first execution: it needs only the
/// definition, not bound work or a host snapshot. Running it at import means an unrunnable diagram is rejected before
/// it is ever persisted, naming the missing capability and the elements that need it, rather than surfacing as an
/// incident the first time the workflow runs. <c>BpmnGraph.Build</c> itself is deliberately not called here: building
/// the graph also validates structural invariants that belong to the runtime module's own execution path
/// (<c>Elsa.Bpmn.Hosting.BpmnScopeHost</c>), and re-running that here would duplicate it outside the module that owns
/// it.
/// </para>
/// </remarks>
public sealed class BpmnInterchangeDocumentService(BpmnXmlReader reader, BpmnXmlWriter writer, BpmnWorkBinder binder, IWorkflowDefinitionImporter importer)
{
    /// <summary>The workflow definition custom property the original BPMN XML is carried under, for <see cref="Export"/>.</summary>
    public const string SourceXmlCustomPropertyKey = "Bpmn:SourceXml";

    /// <summary>
    /// The host capabilities this deployment's BPMN runtime declares.
    /// </summary>
    /// <remarks>
    /// This mirrors <c>Elsa.Bpmn.Hosting.BpmnScopeHost.Capabilities</c> exactly, and for the same reason that constant
    /// gives for not simply writing <see cref="BpmnHostCapabilities.Full"/>: <c>Full</c> would silently grow to cover a
    /// capability a later library version adds and the runtime host has never implemented, which would turn an
    /// import-time approval into a runtime failure — the opposite of what capability refusal at import is for. That
    /// constant is internal to <c>Elsa.Bpmn</c> (the runtime module, kept free of this package's dependency closure per
    /// D12) and cannot be read from here directly, so it is restated. <b>If <c>BpmnScopeHost.Capabilities</c> ever
    /// changes, this must change with it.</b>
    /// </remarks>
    public static readonly BpmnHostCapabilities DeclaredHostCapabilities =
        BpmnHostCapabilities.SubtreeCancellation | BpmnHostCapabilities.ScopeSignalling | BpmnHostCapabilities.IterationScopes | BpmnHostCapabilities.ScopeVariables;

    /// <summary>Every individually named capability, for turning a <see cref="BpmnHostCapabilities"/> flag set into readable names.</summary>
    public static readonly IReadOnlyList<BpmnHostCapabilities> IndividualCapabilities =
    [
        BpmnHostCapabilities.SubtreeCancellation,
        BpmnHostCapabilities.ScopeSignalling,
        BpmnHostCapabilities.IterationScopes,
        BpmnHostCapabilities.ScopeVariables
    ];

    /// <summary>
    /// Reports what a document contains and what a read would cost, without persisting anything.
    /// </summary>
    /// <exception cref="BpmnInterchangeException">The document cannot be read at all.</exception>
    public BpmnImportAnalysis Analyze(string xml) => reader.Analyze(xml, new BpmnImportOptions());

    /// <summary>
    /// Reads a document, refuses it if the host cannot run what it declares, and binds it into the <see cref="BpmnProcess"/>
    /// scope a workflow definition's root becomes.
    /// </summary>
    /// <param name="xml">The BPMN 2.0 XML to import.</param>
    /// <param name="definitionId">The workflow definition to update, or <c>null</c>/empty to create a new one.</param>
    /// <param name="name">The workflow definition's display name, defaulting to the process's own BPMN name or id.</param>
    /// <param name="processId">
    /// The process to bind when the document declares more than one; not needed when it declares exactly one.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="BpmnInterchangeException">The document cannot be read, or declares more than one process and <paramref name="processId"/> does not pick one.</exception>
    /// <exception cref="BpmnCapabilityException">The document needs a host capability this deployment does not declare.</exception>
    /// <exception cref="Exceptions.BpmnBindingException">A work binding cannot be turned into an Elsa activity.</exception>
    public async Task<BpmnDocumentImportResult> ImportAsync(string xml, string? definitionId, string? name, string? processId, CancellationToken cancellationToken)
    {
        var result = reader.Read(xml, new BpmnImportOptions { ProcessId = processId });
        var rootDefinition = ResolveRootDefinition(result.Definitions, processId);

        EnsureCapabilitiesSatisfied(rootDefinition, result.Bindings);

        var process = binder.Bind(rootDefinition, result.Bindings);

        // Whoever composes a bound scope into a workflow says explicitly that it is an entry point; an import is
        // exactly that, for the process the caller asked to import.
        process.IsRootScope = true;

        var model = new WorkflowDefinitionModel
        {
            DefinitionId = definitionId ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(name) ? rootDefinition.Name ?? rootDefinition.ProcessId : name,
            Root = process,
            CustomProperties = new Dictionary<string, object> { [SourceXmlCustomPropertyKey] = xml }
        };

        var importResult = await importer.ImportAsync(new SaveWorkflowDefinitionRequest { Model = model, Publish = false }, cancellationToken);

        return new BpmnDocumentImportResult(importResult, result.Analysis);
    }

    /// <summary>
    /// Writes the document a workflow definition was imported from back out as BPMN 2.0 XML, through the same
    /// reader-then-writer path <see cref="ImportAsync"/> used, so retained extension elements, foreign attributes and
    /// BPMN DI layout come back exactly as the reader retained them.
    /// </summary>
    /// <param name="xml">The BPMN 2.0 XML carried on the workflow definition's <see cref="SourceXmlCustomPropertyKey"/> custom property.</param>
    /// <exception cref="BpmnInterchangeException">The document cannot be read at all.</exception>
    public byte[] Export(string xml)
    {
        var result = reader.Read(xml, new BpmnImportOptions());
        var document = writer.Write(result, new BpmnExportOptions());

        return Encoding.UTF8.GetBytes(document);
    }

    private static BpmnProcessDefinition ResolveRootDefinition(BpmnDefinitions definitions, string? processId)
    {
        if (!string.IsNullOrWhiteSpace(processId))
        {
            // BpmnImportOptions.ProcessId already made the read fail fast if the document does not declare this
            // process, so finding it here can only fail if that guarantee itself changes.
            return definitions.Processes.First(process => string.Equals(process.ProcessId, processId, StringComparison.Ordinal));
        }

        if (definitions.Processes.Count == 1)
            return definitions.Processes[0];

        var declared = string.Join(", ", definitions.Processes.Select(process => process.ProcessId));

        throw new BpmnInterchangeException(
            $"The document declares {definitions.Processes.Count} processes ({declared}); specify which one to import.");
    }

    /// <summary>
    /// Refuses the definition, naming the missing capability and the offending element ids, when it or any process
    /// nested inside it needs a host capability <see cref="DeclaredHostCapabilities"/> does not cover.
    /// </summary>
    private static void EnsureCapabilitiesSatisfied(BpmnProcessDefinition definition, IReadOnlyList<BpmnWorkBinding> bindings) =>
        EnsureCapabilitiesSatisfied(definition, bindings, DeclaredHostCapabilities);

    /// <summary>
    /// Refuses the definition, naming the missing capability and the offending element ids, when it or any process
    /// nested inside it needs a host capability <paramref name="available"/> does not cover.
    /// </summary>
    /// <remarks>
    /// Takes the available capability set as a parameter, rather than reading <see cref="DeclaredHostCapabilities"/>
    /// directly, so a test can prove the refusal — and the walk into nested processes below — without a document that
    /// needs a capability this deployment's runtime host has never declared, which the current library version
    /// cannot produce because <see cref="DeclaredHostCapabilities"/> already covers every capability it defines.
    /// <para>
    /// A nested process is a separate scope with its own graph at execution time, so — mirroring that — it is
    /// analyzed separately here too, walking every <see cref="BpmnWorkBinding.NestedProcess"/> binding whose owner is
    /// the definition just checked.
    /// </para>
    /// </remarks>
    internal static void EnsureCapabilitiesSatisfied(BpmnProcessDefinition definition, IReadOnlyList<BpmnWorkBinding> bindings, BpmnHostCapabilities available)
    {
        BpmnCapabilityRequirements.Analyze(definition).ThrowIfUnmet(available, definition.ProcessId);

        foreach (var nested in bindings.OfType<BpmnWorkBinding.NestedProcess>())
        {
            if (string.Equals(nested.ProcessId, definition.ProcessId, StringComparison.Ordinal))
                EnsureCapabilitiesSatisfied(nested.Definition, bindings, available);
        }
    }
}

/// <summary>The outcome of <see cref="BpmnInterchangeDocumentService.ImportAsync"/>: the persisted definition, plus what the read cost.</summary>
public sealed record BpmnDocumentImportResult(ImportWorkflowResult ImportResult, BpmnImportAnalysis Analysis);
