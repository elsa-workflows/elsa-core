# REST API Contract: Output Converter Descriptors

## List compatible descriptors

```http
GET /descriptors/output-converters?sourceType={typeName}&destinationType={typeName}
```

Authorization:

```text
read:* OR read:output-converters
```

Both query parameters are required registered type aliases or resolvable safe type names.

### Successful response

```json
{
  "items": [
    {
      "id": "sample.to-text",
      "sourceTypeName": "Sample.Source",
      "resultTypeName": "String",
      "displayName": "Convert to text",
      "description": "Formats the source as text.",
      "settingsSchema": {
        "type": "object",
        "properties": {
          "format": {
            "type": "string",
            "enum": ["compact", "indented"]
          }
        }
      }
    }
  ]
}
```

### Validation responses

- `400`: either type parameter is missing or cannot be resolved.
- `403`: caller lacks descriptor permission when API security is enabled.

### Safety

The response never contains converter implementation types, service keys beyond the public ID, instances, factories, lifetimes, or settings values from workflow definitions.

## API client

```csharp
public interface IOutputConvertersApi
{
    [Get("/descriptors/output-converters")]
    Task<ListOutputConvertersResponse> ListAsync(
        [Query] ListOutputConvertersRequest request,
        CancellationToken cancellationToken = default);
}
```

`ListOutputConvertersRequest` contains `SourceType` and `DestinationType`. The response model mirrors the safe descriptor shape using strings and `JsonElement?`.
