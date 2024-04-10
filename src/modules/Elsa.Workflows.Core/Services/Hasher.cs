using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Services;

/// <inheritdoc />
public class Hasher : IHasher
{
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="Hasher"/> class.
    /// </summary>
    public Hasher(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _serializerOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        _serializerOptions.Converters.Add(new TypeJsonConverter(wellKnownTypeRegistry));
        _serializerOptions.Converters.Add(new ExcludeFromHashConverterFactory());
    }
    
    /// <inheritdoc />
    public string Hash(string value)
    {
        using var sha = SHA256.Create();
        return Hash(sha, value);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize(Object, Type, JsonSerializerOptions)")]
    public string Hash(params object?[] values)
    {
        var strings = values.Select(Serialize).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var input = string.Join("|", strings);
        return Hash(input);
    }

    private static string Hash(HashAlgorithm hashAlgorithm, string input)
    {
        var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(data);
    }
    
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize(Object, Type, JsonSerializerOptions)")]
    private string Serialize(object? payload)
    {
        if(payload == null)
            return string.Empty;
        
        if(payload is string s)
            return s;
        
        return JsonSerializer.Serialize(payload, payload.GetType(), _serializerOptions);
    }
}