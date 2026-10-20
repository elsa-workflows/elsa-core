# Keep BPMN XML as the source of truth for BPMN workflows

Date: 2026-10-20

## Status

Accepted

## Context

A BPMN workflow is stored as an ordinary JSON workflow definition whose root is a `BpmnProcess`. The document it came
from is kept beside it in custom properties (`Bpmn:SourceXml`, `Bpmn:SourceVersion`, `Bpmn:SourceProcessId`,
`Bpmn:SourceGraphHash`). Export returns that stored document, never one rebuilt from the graph, because rebuilding would
lose foreign extensions, unknown elements and BPMN DI.

Studio edits through `GET`/`PUT bpmn/definitions/{id}/document`, which exchange the library's `bpmnDefinitions` JSON
rather than XML. That JSON does not carry the bodies of subprocesses, transactions or event subprocesses; `PUT`
restores them from the stored XML by element id. Nothing inside a nested scope can therefore be edited (#8076, blocked
by valence-works/bpmn#22).

A bpmn-js modeler (see [Build the BPMN modeler on bpmn-js](2026-10-20-bpmn-modeler-on-bpmn-js.md)) reads and writes
complete BPMN XML, including subprocess bodies.

## Decision

The BPMN XML document, with its `elsa:` extensions, is the source of truth for a BPMN workflow. The Elsa activity graph
is derived from it on every save and is never edited directly.

- Add `PUT bpmn/definitions/{definitionId}/xml`. It takes a complete `.bpmn` document and runs it through the same path
  as the document `PUT`: analyze, capability and duplicate-id checks, bind, and save as a new draft.
- It requires `If-Match` with the same ETag semantics as the document endpoints, and it keeps the definition's name,
  description, variables, inputs, outputs, options and other custom properties, as the document `PUT` does.
- Add a way to create an empty BPMN workflow: `POST bpmn/definitions` with a name returns a new draft holding a minimal
  process (one plain start event and one end event with DI), so authoring does not depend on uploading a file.
- The document `GET`/`PUT` pair stays for compatibility with the shipped Studio. The modeler does not use it.

## Consequences

- Subprocess contents become editable without waiting for valence-works/bpmn#22, because the XML carries them.
- Export returns exactly what the modeler last saved, so it is never stale after a modeler edit.
- A save sends the whole document. That is acceptable for BPMN-sized documents and keeps one write path.
- Saving the derived graph through the ordinary workflow definition endpoints remains refused, as Studio does today.

## Alternatives considered

- **Keep the JSON document API and wait for valence-works/bpmn#22.** Leaves subprocess editing blocked on the library and
  needs a JSON-to-bpmn-js mapping in Studio.
- **Make the Elsa graph the source of truth and generate XML on export.** Loses layout and foreign content on every
  export. #7909 rejected this for the same reason.
