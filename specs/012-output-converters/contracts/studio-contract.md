# Studio Authoring Contract

## Binding editor

For each activity output:

1. The existing destination selector retains destination identity and declared type metadata.
2. When a typed destination is selected, Studio queries compatible descriptors using the output and destination type names.
3. Studio displays an optional Converter selector with a None choice.
4. Selecting a converter writes only its ID and settings into the activity output JSON.
5. Clearing the converter removes the optional converter object.
6. Changing the destination clears a converter only when it is no longer compatible.
7. Unrelated activity edits preserve converter JSON exactly.

## Settings editor

- With a supported object JSON Schema, render fields for string, number, integer, boolean, enum, required, title, description, and default.
- With no schema or unsupported schema constructs, render a raw JSON object editor.
- Malformed or non-object JSON is rejected locally.
- Server definition-validation messages remain authoritative and are surfaced to the author.
- Read-only workspaces cannot modify converter selection or settings.

## Loading and compatibility

- Cancel or ignore stale descriptor requests when output/destination selection changes.
- If descriptor discovery is unavailable on an older server, hide or disable new controls without deleting persisted converter configuration.
- If a persisted Converter ID is unknown, display the ID and validation state rather than silently clearing it.
- Studio-owned labels and errors are localized; server display text falls back to the Converter ID.

## Independent verification

- Query arguments use the selected declared source and destination types.
- Selecting, configuring, saving, reopening, and clearing round-trip correctly.
- Destination changes invalidate only incompatible converter selections.
- Read-only, unavailable-server, unknown-ID, and malformed-settings states preserve workflow data.
