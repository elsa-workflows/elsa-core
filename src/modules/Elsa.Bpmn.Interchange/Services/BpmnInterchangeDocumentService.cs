using System.Text;
using Bpmn.Interchange;
using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Hosting;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Bpmn.Interchange.Services;

/// <summary>
/// The one code path the Analyze, Import, Export and document (<see cref="ReadDocument"/>/<see cref="ImportDocumentAsync"/>)
/// endpoints all sit on top of.
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
/// <b>Export is only ever the document as imported — through whichever path last imported it.</b>
/// <see cref="Export(WorkflowDefinition)"/> never reconstructs a document from the Elsa activity tree; it always
/// returns the source text the most recent successful <see cref="ImportAsync"/> stored. An edit made through Elsa's
/// own designer, without going back through BPMN, is therefore never reflected — the graph moves on but the stored
/// source describes the document as it stood before that edit, which is why staleness has to be provable rather than
/// assumed; see <see cref="SourceVersionCustomPropertyKey"/>. An edit made through <see cref="ImportDocumentAsync"/>
/// is different: it re-imports, so the stored source and the returned graph both move together, and
/// <see cref="Export(WorkflowDefinition)"/> reflects it immediately afterward.
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
/// while the stored BPMN text still describes the pre-edit document. What decides that is the graph itself, not the
/// version: <see cref="SourceGraphHashCustomPropertyKey"/> records a content hash of the graph at the moment of
/// import, and once it is present it alone says whether the stored source is stale, because it is the only one of
/// the two markers a metadata-only save (a rename, a variable edit) and a graph-changing save always disagree on — a
/// version bump does not, by itself, mean the graph moved, and it does move without a version bump when an
/// unpublished draft is saved in place. <see cref="SourceVersionCustomPropertyKey"/> is what a definition imported
/// before the graph hash existed falls back to.
/// </para>
/// <para>
/// <b>A whole-definition import and a document edit disagree about what else gets replaced.</b> <see cref="ImportAsync"/>
/// builds the <c>WorkflowDefinitionModel</c> from the document alone, so <c>Import</c> — which persists a whole
/// definition from a document, name and all — intentionally replaces the definition's name, description, variables,
/// inputs, outputs, outcomes, options, tool version, read-only flag and custom properties with what that model carries.
/// <see cref="ImportDocumentAsync"/> is different: it edits the BPMN document of an <em>existing</em> definition, so
/// it passes that definition to the shared import logic's <c>preserveMetadataFrom</c> parameter, which carries all
/// of the above onto the result unchanged. Either way, only the bound activity graph and the four custom properties
/// this service owns (<see cref="SourceXmlCustomPropertyKey"/>, <see cref="SourceVersionCustomPropertyKey"/>,
/// <see cref="SourceProcessIdCustomPropertyKey"/>, <see cref="SourceGraphHashCustomPropertyKey"/>) come from the
/// import itself.
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
    IWorkflowDefinitionStore store,
    VariableDefinitionMapper variableDefinitionMapper)
{
    /// <summary>The workflow definition custom property the original BPMN XML is carried under, for <see cref="Export(WorkflowDefinition)"/>.</summary>
    public const string SourceXmlCustomPropertyKey = "Bpmn:SourceXml";

    /// <summary>
    /// The workflow definition custom property <see cref="ImportAsync"/> records the definition's own version number
    /// under, at the moment it stores <see cref="SourceXmlCustomPropertyKey"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Export(WorkflowDefinition)"/> compares this against the definition's current version to tell a
    /// still-current source from a stale one, but only for a definition that carries no <see cref="SourceGraphHashCustomPropertyKey"/> —
    /// imported before that marker existed. Once the graph hash is present, it alone decides staleness and this
    /// comparison is skipped: a version bump does not, by itself, mean the graph changed, and a metadata-only save
    /// (a rename, a variable edit) that carries a definition from a published version N to draft N+1 must not be
    /// judged stale on version alone when the graph the stored source describes has not moved.
    /// </remarks>
    public const string SourceVersionCustomPropertyKey = "Bpmn:SourceVersion";

    /// <summary>
    /// The workflow definition custom property <see cref="ImportAsync"/> records the <c>processId</c> it bound the
    /// definition's root scope from, at the moment it stores <see cref="SourceXmlCustomPropertyKey"/>.
    /// </summary>
    /// <remarks>
    /// A document that declares more than one <c>&lt;process&gt;</c> needs a <c>processId</c> to disambiguate which
    /// one a re-import should bind (see <see cref="ResolveRootDefinition"/>); this is what lets
    /// <see cref="ImportDocumentAsync"/> re-import the same process a multi-process document was originally imported
    /// from, without asking the caller to say so again on every edit. Recorded unconditionally, including for a
    /// single-process document, so this is always derivable the same way rather than only when it happens to matter.
    /// </remarks>
    public const string SourceProcessIdCustomPropertyKey = "Bpmn:SourceProcessId";

    /// <summary>
    /// The workflow definition custom property <see cref="ImportAsync"/> records a content hash of the definition's
    /// serialized activity graph (<see cref="WorkflowDefinition.StringData"/>) under, at the moment it stores
    /// <see cref="SourceXmlCustomPropertyKey"/>.
    /// </summary>
    /// <remarks>
    /// An unpublished draft is saved in place — same row, same version — so a designer save of the draft that edits
    /// a bound activity's inputs changes <see cref="WorkflowDefinition.StringData"/> without changing
    /// <see cref="WorkflowDefinition"/>'s own <c>Version</c>, which <see cref="SourceVersionCustomPropertyKey"/> alone
    /// cannot tell apart from no change at all. Once this marker is present, <see cref="Export(WorkflowDefinition)"/>
    /// and <see cref="ReadDocument"/> decide staleness from it alone: the stored source is current exactly when the
    /// current graph hashes to the value recorded here, whether or not the version has also changed. That matters
    /// the other way around too — a metadata-only save (a rename, a variable edit) bumps a published definition to a
    /// new draft version without touching the graph, and must not be judged stale on version alone once the graph
    /// hash says the document still describes it exactly. Computed with the same hashing
    /// <see cref="Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document.BpmnDocumentETag"/> uses for the graph field, via
    /// <see cref="BpmnContentHash"/>, so the two never disagree about what "the graph changed" means.
    /// <para>
    /// A definition imported before this marker existed carries no value for this key; <see cref="ResolveSourceXml"/>
    /// falls back to the version-only check for it rather than refusing every definition imported under the older
    /// behaviour.
    /// </para>
    /// </remarks>
    public const string SourceGraphHashCustomPropertyKey = "Bpmn:SourceGraphHash";

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
    public Task<BpmnDocumentImportResult> ImportAsync(string xml, string? definitionId, string? name, string? processId, CancellationToken cancellationToken) =>
        ImportCoreAsync(xml, definitionId, name, processId, preserveMetadataFrom: null, cancellationToken);

    /// <summary>
    /// The shared import logic behind both the public <see cref="ImportAsync"/> and <see cref="ImportDocumentAsync"/>:
    /// reads a document, refuses it if the host cannot run what it declares, and binds it into the
    /// <see cref="BpmnProcess"/> scope a workflow definition's root becomes.
    /// </summary>
    /// <param name="xml">The BPMN 2.0 XML to import.</param>
    /// <param name="definitionId">The workflow definition to update, or <c>null</c>/empty to create a new one.</param>
    /// <param name="name">The workflow definition's display name, defaulting to the process's own BPMN name or id.</param>
    /// <param name="processId">
    /// The process to bind when the document declares more than one; not needed when it declares exactly one.
    /// </param>
    /// <param name="preserveMetadataFrom">
    /// When set, the definition this import must otherwise leave untouched: its name, description, variables,
    /// inputs, outputs, outcomes, options, tool version, read-only flag and custom properties are carried onto the
    /// imported definition as-is, and only the bound activity graph and the <see cref="SourceXmlCustomPropertyKey"/>,
    /// <see cref="SourceVersionCustomPropertyKey"/>, <see cref="SourceProcessIdCustomPropertyKey"/> and
    /// <see cref="SourceGraphHashCustomPropertyKey"/> custom properties this method owns change. This is what
    /// <see cref="ImportDocumentAsync"/> passes so the document PUT
    /// edits the BPMN document without silently resetting the rest of the definition; left <c>null</c> for a
    /// whole-definition import, where the model built from the document alone is the intended contract.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="BpmnInterchangeException">The document cannot be read, or declares more than one process and <paramref name="processId"/> does not pick one.</exception>
    /// <exception cref="BpmnCapabilityException">The document needs a host capability this deployment does not declare.</exception>
    /// <exception cref="Exceptions.BpmnBindingException">A work binding cannot be turned into an Elsa activity.</exception>
    private async Task<BpmnDocumentImportResult> ImportCoreAsync(
        string xml,
        string? definitionId,
        string? name,
        string? processId,
        WorkflowDefinition? preserveMetadataFrom,
        CancellationToken cancellationToken)
    {
        var result = reader.Read(xml, new BpmnImportOptions { ProcessId = processId });

        // Before anything below walks into a nested process by matching an id, refuse a document that repeats one:
        // see EnsureElementIdsUnique's remarks for why that walk is otherwise not provably finite. reader.Read itself
        // never recurses this way — it walks the XML's own element tree, not an id lookup — so it is safe to call
        // first and check its result.
        EnsureElementIdsUnique(result.Definitions.Processes, result.Bindings);

        var rootDefinition = ResolveRootDefinition(result.Definitions, processId);

        EnsureCapabilitiesSatisfied(rootDefinition, result.Bindings);

        var process = binder.Bind(rootDefinition, result.Bindings);

        // Whoever composes a bound scope into a workflow says explicitly that it is an entry point; an import is
        // exactly that, for the process the caller asked to import.
        process.IsRootScope = true;

        var model = new WorkflowDefinitionModel
        {
            DefinitionId = definitionId ?? string.Empty,
            Name = preserveMetadataFrom is not null
                ? preserveMetadataFrom.Name
                : string.IsNullOrWhiteSpace(name) ? rootDefinition.Name ?? rootDefinition.ProcessId : name,
            Root = process
        };

        if (preserveMetadataFrom is not null)
        {
            model.Description = preserveMetadataFrom.Description;
            model.Variables = variableDefinitionMapper.Map(preserveMetadataFrom.Variables).ToList();
            model.Inputs = preserveMetadataFrom.Inputs;
            model.Outputs = preserveMetadataFrom.Outputs;
            model.Outcomes = preserveMetadataFrom.Outcomes;
            model.Options = preserveMetadataFrom.Options;
            model.ToolVersion = preserveMetadataFrom.ToolVersion;
            model.IsReadonly = preserveMetadataFrom.IsReadonly;
            model.CustomProperties = new Dictionary<string, object>(preserveMetadataFrom.CustomProperties);
        }

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
            persisted.CustomProperties[SourceProcessIdCustomPropertyKey] = rootDefinition.ProcessId;
            persisted.CustomProperties[SourceGraphHashCustomPropertyKey] = BpmnContentHash.OfGraph(persisted.StringData);
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
    public byte[] Export(WorkflowDefinition definition) => Export(ResolveSourceXml(definition));

    /// <summary>
    /// Resolves the BPMN source a workflow definition was imported from and reads it back as the neutral
    /// <see cref="BpmnDefinitions"/> object model — the same shape <see cref="ImportDocumentAsync"/> accepts back —
    /// through the same reader <see cref="ImportAsync"/> and <see cref="Export(string)"/> use, so retained extension
    /// elements, foreign attributes and BPMN DI layout are present on the returned document exactly as the reader
    /// retained them. Refuses rather than guessing when that source is missing or no longer trustworthy; see this
    /// type's remarks for what "missing" and "stale" mean.
    /// </summary>
    /// <remarks>
    /// <see cref="BpmnDefinitions"/> lists only top-level processes, so the returned document declares every
    /// subprocess element but carries none of their bodies: the library hands those out as work bindings, not as
    /// part of the document. <see cref="ImportDocumentAsync"/> restores them from the stored source rather than from
    /// the document, which is also why nothing inside a nested scope can be edited through the document.
    /// </remarks>
    /// <param name="definition">The workflow definition to read, as read from the store.</param>
    /// <exception cref="BpmnExportUnavailableException">
    /// The definition does not currently carry BPMN source, or it does but the definition has changed since the
    /// source was recorded.
    /// </exception>
    /// <exception cref="BpmnInterchangeException">The stored document cannot be read at all.</exception>
    public BpmnDefinitions ReadDocument(WorkflowDefinition definition)
    {
        var xml = ResolveSourceXml(definition);
        return reader.Read(xml, new BpmnImportOptions()).Definitions;
    }

    /// <summary>
    /// Accepts the whole <see cref="BpmnDefinitions"/> document — the shape <see cref="ReadDocument"/> returns —
    /// writes it back out as BPMN 2.0 XML with <see cref="BpmnXmlWriter"/>, and imports the result through
    /// <see cref="ImportAsync"/>, the same path <c>Import</c> runs. Analyze-then-commit sharing this one code path
    /// with the read side is what keeps a preview unable to disagree with what this actually persists.
    /// </summary>
    /// <remarks>
    /// Unlike a whole-definition import, this edits the BPMN <em>document</em> of an existing definition: the caller
    /// is changing a binding, not replacing the definition. So <paramref name="definitionId"/>'s current metadata —
    /// name, description, variables, inputs, outputs, outcomes, options, tool version, read-only flag and custom
    /// properties other than the ones this service owns — is carried onto the result unchanged; see the shared
    /// import logic's <c>preserveMetadataFrom</c> parameter, which this passes the existing definition to. Only the activity graph
    /// and the <see cref="SourceXmlCustomPropertyKey"/>/<see cref="SourceVersionCustomPropertyKey"/>/
    /// <see cref="SourceProcessIdCustomPropertyKey"/>/<see cref="SourceGraphHashCustomPropertyKey"/> custom properties move.
    /// <para>
    /// Nested scopes come from the stored source, not from <paramref name="document"/>, which cannot carry them (see
    /// <see cref="ReadDocument"/>): every subprocess element <paramref name="document"/> still declares is written back
    /// with the body stored for it, exactly as stored, and a subprocess element it no longer declares takes its stored
    /// body with it. A subprocess element with no stored body — one added by this edit — is written as declared, empty.
    /// </para>
    /// </remarks>
    /// <param name="document">The edited document, deserialized through the library's own JSON converters.</param>
    /// <param name="definitionId">The workflow definition to update.</param>
    /// <param name="processId">
    /// The process to (re-)bind when the document declares more than one; not needed when it declares exactly one.
    /// See <see cref="SourceProcessIdCustomPropertyKey"/> for where a caller re-importing an existing definition
    /// finds the value that was used the first time.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="Exceptions.BpmnDefinitionNotFoundException">
    /// The workflow definition to edit no longer exists — e.g. it was deleted between the PUT endpoint's own
    /// existence/ETag check and this lookup. A missing preservation source must never fall through to the
    /// whole-definition import path, which would silently create a definition under <paramref name="definitionId"/>
    /// with reset metadata instead of reporting that this PUT's target disappeared.
    /// </exception>
    /// <exception cref="BpmnInterchangeException">
    /// The document declares more than one process and <paramref name="processId"/> does not pick one, or it declares a
    /// subprocess element that has a stored body but no bindingRef to write that body back under.
    /// </exception>
    /// <exception cref="BpmnCapabilityException">The document needs a host capability this deployment does not declare.</exception>
    /// <exception cref="Exceptions.BpmnBindingException">A work binding cannot be turned into an Elsa activity.</exception>
    public async Task<BpmnDocumentImportResult> ImportDocumentAsync(BpmnDefinitions document, string definitionId, string? processId, CancellationToken cancellationToken)
    {
        var filter = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();
        var existingDefinition = await store.FindAsync(filter, cancellationToken);

        if (existingDefinition is null)
        {
            throw new BpmnDefinitionNotFoundException(
                $"Workflow definition '{definitionId}' does not exist, so its BPMN document cannot be edited.");
        }

        var storedNestedScopes = StoredNestedScopesStillDeclaredBy(document, existingDefinition);

        // Unlike ImportAsync's xml, writer.Write itself is one of the sites that walks nested processes by matching
        // an id (see EnsureElementIdsUnique's remarks), and it runs before ImportCoreAsync — and the same check
        // inside it — ever sees this document. So it is checked here too, against exactly the inputs writer.Write is
        // about to receive, before that call rather than after it.
        EnsureElementIdsUnique(document.Processes, storedNestedScopes);

        var xml = writer.Write(document, storedNestedScopes);
        return await ImportCoreAsync(xml, definitionId, name: null, processId, preserveMetadataFrom: existingDefinition, cancellationToken);
    }

    /// <summary>
    /// The stored work bindings <see cref="BpmnXmlWriter"/> needs to write every nested scope — embedded subprocess,
    /// transaction or event subprocess — that <paramref name="document"/> still declares back out exactly as
    /// <paramref name="definition"/>'s stored source has it, and nothing for a scope <paramref name="document"/> no
    /// longer declares.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="BpmnDefinitions"/> lists only top-level processes. The body of a nested scope is not on its
    /// subprocess element: the reader hands it out as that element's <see cref="BpmnWorkBinding.NestedProcess"/>
    /// binding, and <see cref="BpmnXmlWriter"/> writes a subprocess whose binding it is not given as empty. The
    /// document <see cref="ReadDocument"/> returns therefore never carries a nested body, so a document written back
    /// without these bindings would silently replace every subprocess with an empty one. The stored source is the
    /// only place the bodies still exist, so they are re-read from it here.
    /// </para>
    /// <para>
    /// A stored body is matched to a subprocess element of the posted document by element id, which BPMN makes
    /// unique across the whole document. It is never matched by bindingRef, so a subprocess element the posted
    /// document removed matches nothing and its stored body is never written back, and a new element that reuses a
    /// removed one's bindingRef does not inherit its body. The writer looks a body up by the element's own
    /// bindingRef, so a kept body is handed over under the bindingRef the posted element carries.
    /// </para>
    /// <para>
    /// Everything bound inside a kept scope comes along with it, not just the bodies of scopes nested further in:
    /// a call activity's <c>vw:waitForCompletion="false"</c>, for one, lives only on its
    /// <see cref="BpmnWorkBinding.CallProcess"/> binding. Nothing inside a kept scope can differ from what is stored,
    /// because the document the client edited never carried it, so the stored bindings describe it exactly.
    /// </para>
    /// <para>
    /// One thing the stored body holds is not its own: <c>Bpmn.Interchange</c> 0.2.0 reads a subprocess's
    /// <c>multiInstanceLoopCharacteristics</c> onto the subprocess element, which is what the writer emits it from and,
    /// at the top level, what the client edits, but also retains a copy as foreign content of the nested process the
    /// element opens. Handed back, that copy would come first on the next read, overriding a marker the client changed
    /// or removed, and every write would add another. So it is dropped from a kept body whenever the element carries a
    /// marker in the model, as stored or as posted, and kept only as the sole record of one the reader could not
    /// interpret. This is tracked upstream as <see href="https://github.com/valence-works/bpmn/issues/21">valence-works/bpmn#21</see>;
    /// remove this workaround once a <c>Bpmn.Interchange</c> release containing that fix is adopted.
    /// </para>
    /// <para>
    /// A definition without stored source has no body to keep; its subprocesses are written as the document declares
    /// them. The document <c>PUT</c> reaches that case only if the source disappears between its <c>If-Match</c> check
    /// and this read: the precondition only matches a stored state a successful <c>GET</c> or <c>PUT</c> described,
    /// and both need the source.
    /// </para>
    /// </remarks>
    /// <exception cref="BpmnInterchangeException">
    /// A subprocess element with a stored body carries no bindingRef, so the writer cannot attach the body to it and
    /// would write the subprocess empty.
    /// </exception>
    private IReadOnlyList<BpmnWorkBinding> StoredNestedScopesStillDeclaredBy(BpmnDefinitions document, WorkflowDefinition definition)
    {
        if (!definition.CustomProperties.TryGetValue<string>(SourceXmlCustomPropertyKey, out var storedXml) || string.IsNullOrEmpty(storedXml))
            return [];

        var stored = reader.Read(storedXml, new BpmnImportOptions());
        var storedBindings = stored.Bindings;

        // Every element carrying a multi-instance marker in the model, as stored or as posted.
        var loopingElementIds = document.Processes
            .Concat(stored.Definitions.Processes)
            .Concat(storedBindings.OfType<BpmnWorkBinding.NestedProcess>().Select(nested => nested.Definition))
            .SelectMany(process => process.Elements)
            .Where(element => element.LoopCharacteristics is not null)
            .Select(element => element.ElementId)
            .ToHashSet(StringComparer.Ordinal);

        var kept = new List<BpmnWorkBinding>();

        foreach (var process in document.Processes)
        {
            foreach (var subprocess in process.Elements.Where(element => element.ElementType == BpmnElementTypes.SubProcess))
            {
                // Last match wins, as it does inside the writer itself, should a malformed document repeat an id.
                var body = storedBindings.OfType<BpmnWorkBinding.NestedProcess>().LastOrDefault(nested => nested.ElementId == subprocess.ElementId);

                if (body is null)
                    continue;

                if (subprocess.BindingRef is null)
                {
                    throw new BpmnInterchangeException(
                        $"Subprocess element '{subprocess.ElementId}' of process '{process.ProcessId}' carries no bindingRef, so the body stored for it cannot be written back with it. "
                        + "The document does not carry a subprocess's body, so writing it without one would silently empty the subprocess. Send the element with the bindingRef the document GET returned.");
                }

                kept.Add(HandOver(body) with { BindingRef = subprocess.BindingRef });
                KeepEverythingBoundInside(body.ElementId);
            }
        }

        return kept;

        // A nested process's bindings name the subprocess element's id as their process (see BpmnWorkBinding.ProcessId).
        void KeepEverythingBoundInside(string scopeId)
        {
            foreach (var binding in storedBindings.Where(binding => binding.ProcessId == scopeId))
            {
                if (binding is not BpmnWorkBinding.NestedProcess nested)
                {
                    kept.Add(binding);
                    continue;
                }

                kept.Add(HandOver(nested));
                KeepEverythingBoundInside(nested.ElementId);
            }
        }

        BpmnWorkBinding.NestedProcess HandOver(BpmnWorkBinding.NestedProcess nested) =>
            loopingElementIds.Contains(nested.ElementId) ? WithoutRetainedLoopMarker(nested) : nested;
    }

    /// <summary>
    /// <paramref name="nested"/> without the copy of its subprocess element's multi-instance marker the reader also
    /// retained on it; see <see cref="StoredNestedScopesStillDeclaredBy"/>'s remarks.
    /// </summary>
    private static BpmnWorkBinding.NestedProcess WithoutRetainedLoopMarker(BpmnWorkBinding.NestedProcess nested)
    {
        var marker = new BpmnQName(BpmnXmlNames.Model.NamespaceName, "multiInstanceLoopCharacteristics");
        var extensions = nested.Definition.Extensions;
        var foreignChildren = extensions.ForeignChildren.Where(child => child.Element.Name != marker).ToList();
        return nested with { Definition = nested.Definition with { Extensions = extensions with { ForeignChildren = foreignChildren } } };
    }

    /// <summary>
    /// The BPMN source a workflow definition was imported from, refusing rather than guessing when it is missing or
    /// no longer trustworthy. See this type's remarks for what "missing" and "stale" mean and why each gets its own
    /// message.
    /// </summary>
    /// <exception cref="BpmnExportUnavailableException">
    /// The definition does not currently carry BPMN source, or it does but the definition has changed since the
    /// source was recorded.
    /// </exception>
    private static string ResolveSourceXml(WorkflowDefinition definition)
    {
        if (!definition.CustomProperties.TryGetValue<string>(SourceXmlCustomPropertyKey, out var xml) || string.IsNullOrEmpty(xml))
        {
            throw new BpmnExportUnavailableException(
                $"Workflow definition '{definition.DefinitionId}' does not currently carry BPMN source, so it cannot be exported as BPMN 2.0 XML. "
                + "Either it was never imported from a BPMN document, or a later save replaced its custom properties wholesale and removed the "
                + $"'{SourceXmlCustomPropertyKey}' entry as a side effect of editing something else.",
                BpmnExportUnavailableReason.NotImported);
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
                + "record a complete, exportable source.",
                BpmnExportUnavailableReason.SourceVersionUnknown);
        }

        // The graph hash, once recorded, is the sole word on staleness: it is unaffected by a metadata-only save
        // (a rename, a variable change) that bumps the definition to a new draft version without touching the graph
        // the stored source describes, which the version check below would otherwise flag as stale even though the
        // document still matches exactly. A definition imported before this marker existed carries no value for it,
        // so it falls back to the version check instead of refusing every definition imported under the older
        // behaviour.
        if (definition.CustomProperties.TryGetValue<string>(SourceGraphHashCustomPropertyKey, out var sourceGraphHash) && !string.IsNullOrEmpty(sourceGraphHash))
        {
            if (sourceGraphHash != BpmnContentHash.OfGraph(definition.StringData))
            {
                throw new BpmnExportUnavailableException(
                    $"Workflow definition '{definition.DefinitionId}' has changed since it was imported from BPMN: its activity graph no longer matches "
                    + "the graph the stored source was imported against. The BPMN source stored on it no longer corresponds to this definition, so "
                    + "exporting it would silently return a document that is not what this definition currently is.",
                    BpmnExportUnavailableReason.SourceStale);
            }
        }
        else if (sourceVersion != definition.Version)
        {
            throw new BpmnExportUnavailableException(
                $"Workflow definition '{definition.DefinitionId}' has changed since it was imported from BPMN (imported at version {sourceVersion}, "
                + $"currently at version {definition.Version}). The BPMN source stored on it no longer corresponds to this definition, so exporting it "
                + "would silently return a document that is not what this definition currently is.",
                BpmnExportUnavailableReason.SourceStale);
        }

        return xml;
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
    /// Refuses a document that declares the same element id more than once, naming the duplicated ids.
    /// </summary>
    /// <remarks>
    /// <para>
    /// BPMN requires every element id to be unique within a document. The library's own reader tolerates a repeat —
    /// <c>BpmnXmlReader</c> walks the XML's own element tree, so it terminates regardless of what any id says — but
    /// nothing downstream of it does: this type's own <c>EnsureCapabilitiesSatisfied</c> and <c>BpmnWorkBinder.BindScope</c>
    /// both find "the nested processes belonging to this scope" by matching <see cref="BpmnWorkBinding.ProcessId"/>
    /// against the scope's own id, and <c>Bpmn.Interchange</c>'s own <c>BpmnXmlWriter</c> does the same by matching
    /// <see cref="BpmnWorkBinding.BindingRef"/>. A <see cref="BpmnWorkBinding.NestedProcess"/>'s own
    /// <see cref="BpmnProcessDefinition.ProcessId"/> is always the element id of the subprocess element that opens
    /// it, so a subprocess nested inside another subprocess that reuses its parent's id makes that lookup find its
    /// own parent — or itself — again on every step down. Each of those three walks then recurses without ever
    /// terminating and crashes the process outright: .NET cannot catch a <see cref="StackOverflowException"/>. This
    /// runs before any of them does, so a document like that is refused rather than crashing the server.
    /// </para>
    /// <para>
    /// Once every element id is unique, that recursion is provably finite without a separate depth guard: a scope's
    /// nested processes can then only ever be the ones its own <see cref="BpmnWorkBinding.ProcessId"/> or
    /// <see cref="BpmnWorkBinding.BindingRef"/> actually names, so the walk can only ever follow the tree the
    /// document's own nesting describes.
    /// </para>
    /// </remarks>
    /// <param name="processes">The document's own top-level process bodies.</param>
    /// <param name="bindings">
    /// Every binding across the same processes, so every subprocess body nested inside them — which is not one of
    /// <paramref name="processes"/> itself, and carries elements <paramref name="processes"/> does not enumerate —
    /// is covered too.
    /// </param>
    /// <remarks>
    /// A top-level <see cref="BpmnProcessDefinition"/>'s own <see cref="BpmnProcessDefinition.ProcessId"/> is a scope
    /// id in exactly the same id-space as every element id below it: <c>EnsureCapabilitiesSatisfied</c>,
    /// <c>BpmnWorkBinder.BindScope</c> and <c>Bpmn.Interchange</c>'s own <c>BpmnXmlWriter</c> all find "the nested
    /// processes belonging to this scope" by matching a <see cref="BpmnWorkBinding.NestedProcess"/>'s owner id
    /// against a <see cref="BpmnProcessDefinition.ProcessId"/> — a top-level process's own <em>id</em>, not one of
    /// its declared elements, so nothing below ever puts it in the pool checked for uniqueness on its own. A
    /// subprocess reusing that id (e.g. <c>&lt;process id="P"&gt;&lt;subProcess id="P"&gt;</c>) makes that lookup
    /// find the top-level scope again instead of terminating — the same class of infinite recursion a repeated
    /// element id causes — so it is added here explicitly, once per top-level process.
    /// <para>
    /// A <em>nested</em> process definition's own <see cref="BpmnProcessDefinition.ProcessId"/> needs no equivalent
    /// addition: it is always exactly the <see cref="BpmnWorkBinding.ElementId"/> of the subprocess
    /// element that opens it, by construction of the library's own reader, and that element id is already in the
    /// pool below as one of its <em>owner</em>'s elements. Adding it a second time would flag every ordinary
    /// subprocess as a duplicate of itself; the legitimate pairing is counted once by not adding it again here.
    /// </para>
    /// </remarks>
    /// <exception cref="BpmnDuplicateElementIdException">An element id, or a top-level process id, is declared more than once.</exception>
    internal static void EnsureElementIdsUnique(IEnumerable<BpmnProcessDefinition> processes, IReadOnlyList<BpmnWorkBinding> bindings)
    {
        var processList = processes as IReadOnlyCollection<BpmnProcessDefinition> ?? processes.ToList();

        var processIds = processList.Select(process => process.ProcessId);

        var elementIds = processList
            .Concat(bindings.OfType<BpmnWorkBinding.NestedProcess>().Select(nested => nested.Definition))
            .SelectMany(process => process.Elements)
            .Select(element => element.ElementId);

        var duplicateIds = processIds
            .Concat(elementIds)
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateIds.Count == 0)
            return;

        throw new BpmnDuplicateElementIdException(
            $"The document declares the same element id more than once, which BPMN requires to be unique: {string.Join(", ", duplicateIds)}. "
            + "This is most often a subprocess nested inside another subprocess that reuses its parent's id. Reading or writing such a document "
            + "cannot be done safely, so it is refused rather than attempted.",
            duplicateIds);
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
