using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.EntityFrameworkCore.ValueConverters;

/// <summary>
/// Converts JSON values to and from the specified type.
/// </summary>
/// <typeparam name="TModel">The type to convert to and from.</typeparam>
public class JsonValueConverter<TModel>(JsonSerializerOptions? options = null) : ValueConverter<TModel, string>(v => JsonSerializer.Serialize(v, options),
    v => JsonSerializer.Deserialize<TModel>(v, options)!,
    new ConverterMappingHints());