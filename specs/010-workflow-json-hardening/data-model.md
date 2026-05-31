# Data Model: Workflow JSON Type Hardening

## SerializationTypeOptions

- Stores workflow JSON aliases mapped to concrete types.
- Stores optional legacy names mapped to the same concrete types.
- Provides default primitive and JSON island aliases needed by workflow payloads.
- Lives in `Elsa.Common` so non-workflow serialization layers can share the same trust boundary.

## SerializationTypeRegistry

- Runtime registry built from `SerializationTypeOptions`.
- Resolves aliases and registered legacy names to types.
- Lists registered types for compatibility resolution.
- Returns preferred aliases for writing new workflow JSON and public descriptor values.

## Workflow Type Identifier

- Alias: preferred stable identifier for new workflow JSON.
- Legacy name: supported compatibility identifier for previously persisted workflow JSON or older clients.
