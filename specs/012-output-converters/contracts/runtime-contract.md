# Runtime Extension Contract

The public API will expose equivalent C# contracts with XML documentation.

```csharp
public sealed record OutputConverterConfiguration(
    string Id,
    JsonElement? Settings = null);

public sealed record OutputConverterDescriptor(
    string Id,
    Type SourceType,
    Type ResultType,
    string DisplayName,
    string? Description = null,
    JsonElement? SettingsSchema = null);

public sealed record OutputConversionContext(
    object Value,
    Type SourceType,
    Type DestinationType,
    JsonElement? Settings);

public interface IOutputConverter
{
    object? Convert(OutputConversionContext context);

    IEnumerable<string> ValidateSettings(JsonElement? settings);
}

public interface IOutputConverterRegistry
{
    IEnumerable<OutputConverterDescriptor> ListAll();

    OutputConverterDescriptor? Find(string id);

    IEnumerable<OutputConverterDescriptor> FindCompatible(
        Type sourceType,
        Type destinationType);
}
```

Registration supports an explicit lifetime:

```csharp
services.AddOutputConverter<TConverter>(
    descriptor,
    ServiceLifetime.Scoped);
```

Required semantics:

- Converter ID lookup is ordinal and case-sensitive.
- Exact and case-only duplicate registrations fail.
- A descriptor is compatible when its source type is assignable from the declared output type and its result type is assignable to the destination type.
- The converter implementation resolves from the active scope using its ID.
- Settings and schema values are cloned before storage/exposure.
- Null native values bypass `Convert`.
- Converter implementations are deterministic and side-effect-free.
