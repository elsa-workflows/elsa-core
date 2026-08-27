using System.Text;
using Bpmn.Interchange;
using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Hosting;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
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
/// on the workflow definition's custom properties. <see cref="Export(string)"/> re-reads that same text through the same
/// reader and hands the resulting <see cref="BpmnImportResult"/> — retained extension elements, foreign attributes,
/// unrecognized children and BPMN DI layout included — straight to <see cref="BpmnXmlWriter"/>. Nothing is
/// reconstructed from the Elsa activity tree, which only carries a bindingRef-to-activityId map and would have to
/// throw away everything the reader retained to get there.
/// </para>
/// <para>
/// <b>Export is only ever the document as imported, and that is a real limitation, not a detail.</b> Until BPMN-aware
/// editing exists (a Studio concern, out of scope for this program), nothing re-serializes edits made through Elsa's
/// own designer back into BPMN — <see cref="Export(WorkflowDefinition)"/> always returns the source text
/// <see cref="ImportAsync"/> stored, never a document reflecting what the definition currently is. That is why it
/// refuses outright, rather than returning something, when it cannot prove that text still matches the definition:
/// see <see cref="SourceVersionCustomPropertyKey"/>.
/// </para>
/// <para>
/// <b>The stored source can go missing or stale after import, and each is refused with its own diagnosis.</b> BPMN
/// source travels on <see cref="SourceXmlCustomPropertyKey"/>, one entry in the same <c>CustomProperties</c>
/// dictionary a workflow edit can — and, through Elsa's own workflow-definition save endpoint, does — replace
/// wholesale. A save that does not carry that key forward removes it as a side effect of editing something else
/// entirely, which is indistinguishable, once it has happened, from a definition that was never imported from BPMN
/// in the first place; <see cref="Export(WorkflowDefinition)"/> says so honestly rather than asserting the document
/// was "never imported", which would be true in one case and false in the other. Separately, a save that DOES carry
/// the key forward can still leave the definition materially changed — the graph, name, or anything else about it —
/// while the stored BPMN text still describes the pre-edit document. <see cref="SourceVersionCustomPropertyKey"/>
/// records the definition's own version at the moment of import for exactly this: if the current version no longer
/// matches, the stored source is stale, and exporting it would silently hand back a document that is not what the
/// caller has, which is worse than refusing.
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
public sealed class BpmnInterchangeDocumentService(
    BpmnXmlReader reader,
    BpmnXmlWriter writer,
    BpmnWorkBinder binder,
    IWorkflowDefinitionImporter importer,
    IWorkflowDefinitionStore store)
{
    /// <summary>The workflow definition custom property the original BPMN XML is carried under, for <see cref="Export(WorkflowDefinition)"/>.</summary>
    public const string SourceXmlCustomPropertyKey = "Bpmn:SourceXml";

    /// <summary>
    /// The workflow definition custom property <see cref="ImportAsync"/> records the definition's own version number
    /// under, at the moment it stores <see cref="SourceXmlCustomPropertyKey"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Export(WorkflowDefinition)"/> compares this against the definition's current version to tell a
    /// still-current source from a stale one. The version number is what <see cref="WorkflowDefinition"/> itself
    /// already exposes for "has this definition changed", so this reuses it rather than inventing a second notion of
    /// change (a content hash, a timestamp) that could disagree with the versioning the rest of the system already
    /// uses.
    /// </remarks>
    public const string SourceVersionCustomPropertyKey = "Bpmn:SourceVersion";

    /// <summary>
    /// The host capabilities this deployment's BPMN runtime declares.
    /// </summary>
    /// <remarks>
    /// Reads <see cref="BpmnRuntimeCapabilities.Declared"/> straight from <c>Elsa.Bpmn</c> — the runtime module,
    /// which already publishes that constant for exactly this reason — rather than restating the flag set here.
    /// A restatement could silently drift from what <c>Elsa.Bpmn.Hosting.BpmnScopeHost</c> actually honours at
    /// execution time, which would mean import-time refusal and runtime behaviour disagreeing: the worse direction
    /// for that drift to go is a document accepted here and only failing the first time it runs.
    /// </remarks>
    public static readonly BpmnHostCapabilities DeclaredHostCapabilities = BpmnRuntimeCapabilities.Declared;

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
            Root = process
        };

        var importResult = await importer.ImportAsync(new SaveWorkflowDefinitionRequest { Model = model, Publish = false }, cancellationToken);

        // The definition's final Version is only known once the importer/publisher has assigned and persisted it —
        // see SourceVersionCustomPropertyKey's remarks for why that value, specifically, is what staleness is judged
        // against. Neither custom property is written until it is known, so both land on this single, explicit save:
        // if it fails or is cancelled, the definition carries neither key, which Export(WorkflowDefinition) reports
        // honestly as "never imported" rather than as a partial import that cannot be diagnosed.
        if (importResult.Succeeded)
        {
            var persisted = importResult.WorkflowDefinition;
            persisted.CustomProperties[SourceXmlCustomPropertyKey] = xml;
            persisted.CustomProperties[SourceVersionCustomPropertyKey] = persisted.Version;
            await store.SaveAsync(persisted, cancellationToken);
        }

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

    /// <summary>
    /// Resolves the BPMN source a workflow definition was imported from and writes it back out, refusing rather than
    /// guessing when that source is missing or no longer trustworthy. See this type's remarks for what "missing" and
    /// "stale" mean and why each gets its own message.
    /// </summary>
    /// <param name="definition">The workflow definition to export, as read from the store.</param>
    /// <exception cref="BpmnExportUnavailableException">
    /// The definition does not currently carry BPMN source, or it does but the definition has changed since the
    /// source was recorded.
    /// </exception>
    /// <exception cref="BpmnInterchangeException">The stored document cannot be read at all.</exception>
    public byte[] Export(WorkflowDefinition definition)
    {
        if (!definition.CustomProperties.TryGetValue<string>(SourceXmlCustomPropertyKey, out var xml) || string.IsNullOrEmpty(xml))
        {
            throw new BpmnExportUnavailableException(
                $"Workflow definition '{definition.DefinitionId}' does not currently carry BPMN source, so it cannot be exported as BPMN 2.0 XML. "
                + "Either it was never imported from a BPMN document, or a later save replaced its custom properties wholesale and removed the "
                + $"'{SourceXmlCustomPropertyKey}' entry as a side effect of editing something else.");
        }

        if (!definition.CustomProperties.TryGetValue<int>(SourceVersionCustomPropertyKey, out var sourceVersion))
        {
            // Distinct from both other refusals: this is not "never imported" (the source text is right there) and
            // not "stale" (there is no version to compare against yet). ImportAsync writes SourceXmlCustomPropertyKey
            // and SourceVersionCustomPropertyKey together, in the single save described in its remarks, so this path
            // is not reachable through import itself; it is kept as a defence against the same combination arising
            // some other way — e.g. custom properties edited or migrated directly, outside ImportAsync — where
            // "whether the source still matches" cannot be verified without a version to compare against.
            throw new BpmnExportUnavailableException(
                $"Workflow definition '{definition.DefinitionId}' carries BPMN source, but not the definition version it was recorded against, so "
                + "whether that source still matches this definition cannot be verified. It does not mean this definition was never imported from "
                + "BPMN, and it does not mean the source is stale — there is simply no version recorded to compare against. Re-import the document to "
                + "record a complete, exportable source.");
        }

        if (sourceVersion != definition.Version)
        {
            throw new BpmnExportUnavailableException(
                $"Workflow definition '{definition.DefinitionId}' has changed since it was imported from BPMN (imported at version {sourceVersion}, "
                + $"currently at version {definition.Version}). The BPMN source stored on it no longer corresponds to this definition, so exporting it "
                + "would silently return a document that is not what this definition currently is.");
        }

        return Export(xml);
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

        var ownedNestedProcesses = bindings
            .OfType<BpmnWorkBinding.NestedProcess>()
            .Where(nested => string.Equals(nested.ProcessId, definition.ProcessId, StringComparison.Ordinal));

        foreach (var nested in ownedNestedProcesses)
            EnsureCapabilitiesSatisfied(nested.Definition, bindings, available);
    }
}

/// <summary>The outcome of <see cref="BpmnInterchangeDocumentService.ImportAsync"/>: the persisted definition, plus what the read cost.</summary>
public sealed record BpmnDocumentImportResult(ImportWorkflowResult ImportResult, BpmnImportAnalysis Analysis);
