# Elsa Workflow Runtime

The workflow runtime executes activities and moves their results into destinations that workflows can consume.

## Language

**Activity Output**:
The native value produced by an activity. Its meaning and type belong to the activity's contract and are not changed by a consumer's binding choices. Activity-output registers, journals, APIs, and diagnostics expose this native value.
_Avoid_: Converted output, bound output

**Output Binding**:
The association that delivers an Activity Output to a variable or workflow output. It may apply at most one explicitly configured Output Converter before delivery. Its optional persisted converter configuration contains only a Converter ID and JSON Converter Settings. Without converter configuration, it follows the existing assignment path unchanged.
_Avoid_: Activity output, output definition

**Output Converter**:
An optional deterministic, side-effect-free transformation selected strictly at an Output Binding. It changes the value delivered by that binding without changing the underlying Activity Output. It does not convert activity inputs or general expression results. Environmental choices such as locale are explicit Converter Settings.
_Avoid_: Activity converter, implicit coercion

**Converter ID**:
The stable semantic identifier by which an Output Binding explicitly selects a registered Output Converter. Matching is ordinal and case-sensitive, while registrations that differ only by case are rejected. Breaking changes to conversion behavior, settings, or result semantics use a new Converter ID.
_Avoid_: Converter type name, converter class

**Converter Settings**:
Optional workflow-specific parameters that refine how the selected Output Converter transforms one Output Binding. They are immutable during conversion.
_Avoid_: Global converter options, converter service configuration

**Converter Descriptor**:
The server-owned, API-discoverable identity and compatibility declaration of an Output Converter, including its supported source type, declared result type, localizable display metadata, and optional JSON Schema for Converter Settings. Source compatibility follows base-class and interface assignability; the result must be assignable to the Destination Type. Display metadata is not persisted with the workflow.
_Avoid_: Converter instance, activity descriptor

**Conversion Context**:
The narrow, immutable input supplied to an Output Converter: the native value, declared source and destination types, and Converter Settings. It does not expose mutable workflow execution state or a service locator.
_Avoid_: Activity execution context, workflow context

**Destination Type**:
The resolvable declared type of the variable or workflow output receiving a Bound Value. `object` is a valid Destination Type; an unknown or untyped destination is not.
_Avoid_: Runtime value type, inferred target

**Output Conversion Error**:
The dedicated activity fault raised when converter resolution, settings validation, compatibility checking, invocation, or result validation fails. It carries structured converter, activity, output, destination, and failure-stage metadata without exposing native values or raw settings by default.
_Avoid_: Assignment error, converter log message

**Bound Value**:
The value delivered only to the destination of an Output Binding after any configured Output Converter has run. It may be null only when the destination permits null.
_Avoid_: Activity output
