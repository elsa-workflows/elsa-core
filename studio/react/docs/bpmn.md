# BPMN import and export

From Workflow definitions, choose **Import BPMN**, select a `.bpmn` file up to
10 MB, and review the server's Info, Degraded and Dropped findings. Element and
process IDs identify affected parts. Dropped elements will not run; Degraded
elements behave differently from their authored definition. A document with more
than one process requires an explicit process selection before **Import** enables.
Canceling the dialog persists nothing. Confirming imports the same selected file
and opens a read-only view of the new draft. This view preserves the native
BPMN root and never normalizes or autosaves it. Export remains available; canvas
editing is separate work in #8929.

The API remains the source of truth for supported BPMN capabilities. Capability
refusals list missing capabilities and offending elements. Unsupported expression
conditions list sequence-flow IDs and explain why unconditional execution would
change routing. These messages use stable server error codes and retain the
server's explanation. Unknown codes and older-server errors show the ordinary
server error rather than guessing their meaning.

Use a definition row's **Actions → Export BPMN** to download the latest imported
document. The confirmation warns that bindings and expressions are included.
Export is refused when BPMN source is absent, its recorded version is unknown,
or it has become stale after workflow edits. The browser downloads the server's
XML bytes; the server may normalize XML and add missing diagram layout. Reimport
preserves the process/flow model, and a subsequent export is stable. It does not
promise byte equality with an original document that omitted layout.

Import requires `workflows/definitions:write`, export/analyze require `:view`,
and the backend must have BPMN interchange enabled. Existing read-only server
configuration hides the import action. Requests share the configured API origin,
authentication and tenant headers; closing a dialog cancels its outstanding request.

The public API client exposes `client.bpmn.analyze(file, signal)`,
`client.bpmn.import(file, options, signal)` and
`client.bpmn.export(definitionId, signal)`, with typed analysis/results and optional
process/name/definition IDs for API consumers. The UI imports new drafts, so it
does not overwrite an existing definition.

Browser tests cover findings, process choice, cancellation of pending analysis and
export requests, native-root preservation without autosave, capability/condition
refusals, stale/missing-source refusals, unknown error fallback and download/reimport.
To check a disposable real backend, set `ELSA_TEST_API_URL`, `ELSA_TEST_USERNAME`
and `ELSA_TEST_PASSWORD`, then run `pnpm test:bpmn`. This checks analysis,
import/export/reimport, stable canonical XML, structured expression-condition
refusal and cleanup of its temporary definitions.

This implements interchange for [#8853](https://github.com/elsa-workflows/elsa-core/issues/8853).
An editable BPMN diagram canvas is separate work in #8929.
