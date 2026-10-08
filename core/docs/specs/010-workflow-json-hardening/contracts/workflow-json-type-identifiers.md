# Contract: Workflow JSON Type Identifiers

## Identifier Rules

- New workflow JSON writes aliases when a type is registered with a workflow JSON alias.
- Compatibility reads accept registered aliases and registered legacy names.
- Unknown CLR names are rejected.
- Abstract, interface, open generic, and inappropriate collection targets are rejected for polymorphic object materialization.
- Supported collection aliases are limited to known collection wrappers closed over registered element types.

## Incident Strategy Descriptor

`GET /descriptors/incident-strategies` returns `typeName` values from the shared serialization type registry.

Clients should persist or submit returned `typeName` values unchanged. During the compatibility window, existing CLR names registered as legacy identifiers remain accepted when workflow JSON is read.
