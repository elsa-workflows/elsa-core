# Data Model: Extensible Activity Output Converters

## Output Binding

Existing `Output`/`Output<T>` binding model with one new optional relationship:

- `Converter`: zero or one Converter Configuration
- Existing memory reference remains the destination identity
- Native output type remains `T` or `object` for untyped outputs

Validation:

- Converter configuration requires a resolvable destination.
- No configuration preserves existing serialization and behavior.

## Converter Configuration

- `Id`: required non-empty Converter ID
- `Settings`: optional JSON object, cloned when persisted or invoked

Serialization:

```json
{
  "typeName": "String",
  "memoryReference": {
    "id": "resultVariable"
  },
  "converter": {
    "id": "sample.to-text",
    "settings": {
      "format": "compact"
    }
  }
}
```

## Output Converter Registration

- `Descriptor`: immutable Converter Descriptor
- `ServiceKey`: exact Converter ID used for keyed DI resolution
- `ServiceLifetime`: transient, scoped, or singleton registration lifetime

Uniqueness:

- Exact ordinal IDs are unique.
- IDs differing only by case are also prohibited.

## Converter Descriptor

- `Id`: stable semantic identity
- `SourceType`: supported source CLR type
- `ResultType`: declared result CLR type
- `DisplayName`: discoverable display text
- `Description`: optional discoverable description
- `SettingsSchema`: optional cloned JSON Schema

API projection replaces CLR types with registered aliases or safe type names and omits service registration data.

## Output Conversion Context

- `Value`: non-null native Activity Output
- `SourceType`: declared Activity Output type
- `DestinationType`: resolved declared destination type
- `Settings`: immutable/cloned JSON settings

The context has no workflow execution object or service provider.

## Destination

- `Id`: memory-reference or workflow-output identity
- `Type`: resolved CLR type
- `AllowsNull`: true for reference types and `Nullable<T>`, false for other value types
- `Kind`: variable or workflow output

Definition resolution walks the nearest variable-container scope and then workflow outputs. Runtime resolution uses declared memory-block metadata.

## Output Conversion Error

- `ConverterId`
- `Stage`: Resolution, SettingsValidation, SourceCompatibility, Invocation, or ResultValidation
- `ActivityId`
- `ActivityType`
- `OutputName`
- `DestinationId`
- `SourceTypeName`
- `DestinationTypeName`
- `InnerException`: optional originating converter exception

Safe fields are copied to persisted exception metadata. Native values and settings are never included.

## State transitions

```text
Unconfigured Binding
  └─ assign native value using existing path

Configured Binding
  ├─ record native output
  ├─ native null → validate destination nullability → write null
  └─ non-null
      ├─ resolve registration and destination
      ├─ validate source, destination, and settings
      ├─ invoke converter
      ├─ validate result and nullability
      ├─ success → write Bound Value
      └─ failure → fault activity; destination unchanged
```
