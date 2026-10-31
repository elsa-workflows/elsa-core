# Research: Workflow JSON Type Hardening

## Decision: Dedicated Workflow JSON Registry

Use a workflow-specific registry and options object for `TypeJsonConverter`, `PolymorphicObjectConverter`, workflow state serialization, bookmark payload serialization, trigger comparison, hashing, and descriptor contracts.

**Rationale**: The issue explicitly rejects using `ExpressionOptions` as the workflow JSON trust boundary. A dedicated registry makes the allowed workflow JSON surface explicit while leaving expression aliases available for expression evaluation and designer variable metadata.

**Alternatives considered**: Reusing `IWellKnownTypeRegistry` was rejected because it is fed by `ExpressionOptions`. Direct `Type.GetType` fallback was rejected because it can load arbitrary CLR names.

## Decision: Explicit Legacy Compatibility Names

Compatibility reads accept aliases and registered legacy names, including simple assembly-qualified names, full assembly-qualified names for registered types, and supported collection wrappers over registered element types.

**Rationale**: Existing persisted workflows may contain CLR names, but the trust boundary must be the registration list, not assembly probing.

**Alternatives considered**: A broad assembly allow-list was rejected for this slice because it is harder to reason about and can accidentally expose unrelated types from trusted assemblies.

## Decision: Alias-First Public Descriptor Contract

Incident strategy descriptors should emit the workflow JSON alias for registered strategies while keeping legacy CLR names readable on input during the compatibility window.

**Rationale**: The reported dropdown failure came from descriptors returning CLR names while hardened reads expected aliases. Emitting aliases aligns new clients with hardened serialization without breaking old payloads.

**Alternatives considered**: Keeping CLR names in descriptors was rejected because it perpetuates the inconsistent contract.
