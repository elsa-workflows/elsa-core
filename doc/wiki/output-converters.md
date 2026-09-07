# Output Converters

Output converters let an extension transform an activity's native output for one bound variable or workflow output. Conversion is explicit and opt-in: activity-output registers and diagnostics keep the native value, while only the binding destination receives the converted value.

## Implement And Register A Converter

Converters are synchronous, deterministic, and side-effect free. Their context contains only the native value, declared source and destination types, and immutable JSON settings.

```csharp
using System.Globalization;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;

public sealed class NumberToTextConverter : IOutputConverter
{
    public object Convert(OutputConversionContext context)
    {
        var format = context.Settings?.TryGetProperty("format", out var value) == true
            ? value.GetString()
            : null;

        return ((decimal)context.Value).ToString(format, CultureInfo.InvariantCulture);
    }
}

using var schemaDocument = JsonDocument.Parse(
    """{"type":"object","properties":{"format":{"type":"string"}}}""");

services.AddOutputConverter<NumberToTextConverter>(
    new OutputConverterDescriptor(
        "sample.number-to-text.v1",
        typeof(decimal),
        typeof(string),
        "Number to text",
        "Formats a decimal using an explicit invariant format.",
        schemaDocument.RootElement));
```

The converter ID is a persisted public contract. Keep lookup case-sensitive, do not reuse an ID for breaking behavior or settings changes, and register a new versioned ID instead.

The default service lifetime is scoped. Elsa resolves the implementation from the active workflow execution scope; the registry caches descriptors and registrations, not converter instances.

## Configure A Binding

Converter configuration belongs to an `Output<T>` binding:

```csharp
activity.Result = new Output<decimal>(targetVariable)
{
    Converter = new OutputConverterConfiguration(
        "sample.number-to-text.v1",
        JsonDocument.Parse("""{"format":"0.00"}""").RootElement)
};
```

The serialized binding adds one optional object:

```json
{
  "typeName": "Decimal",
  "memoryReference": {
    "id": "formatted-total"
  },
  "converter": {
    "id": "sample.number-to-text.v1",
    "settings": {
      "format": "0.00"
    }
  }
}
```

Bindings without a converter omit `converter` and retain the existing assignment path.

## Validation And Failure Behavior

At definition validation and again at runtime, Elsa checks:

- the binding has a resolvable variable or workflow-output destination;
- the converter ID is registered;
- the declared output type is compatible with the converter source type;
- the converter result type is assignable to the destination;
- JSON Schema and converter-owned settings validation pass.

Null native values bypass converter invocation and are delivered only to destinations that permit null. Conversion and result validation finish before Elsa writes the destination, so a failure leaves it unchanged.

Runtime failures fault through Elsa's normal activity fault pipeline as `OutputConversionException`. Persisted exception state includes safe structured identities and the failure stage; it excludes native values, raw settings, and converter exception details.

## Discovery API And Studio

Studio queries:

```http
GET /descriptors/output-converters?sourceType=Decimal&destinationType=String
```

The endpoint requires `workflows/descriptors/output-converters:view`. It returns compatible IDs, declared type names, display metadata, and optional settings schemas. It never exposes converter instances, implementation types, or service lifetimes.

Studio offers schema-driven fields for simple object schemas and a raw JSON-object editor otherwise. Unknown persisted converter IDs remain visible, and older servers that do not expose discovery do not cause Studio to delete existing configuration.

## Operational Guidance

- Make locale, timezone, rounding, and similar environmental choices explicit settings.
- Do not perform I/O or mutate workflow state from a converter.
- Treat converter removal as deployment drift: previously published workflows that reference it will fault at assignment.
- Use activity inputs or an explicit activity when transformation is asynchronous or has side effects.

The design decisions are recorded in [ADR 0011](../adr/0011-output-conversion-at-binding-is-synchronous.md), [ADR 0012](../adr/0012-output-converters-use-explicit-stable-identities.md), and [ADR 0013](../adr/0013-output-converter-discovery-is-server-owned.md).
