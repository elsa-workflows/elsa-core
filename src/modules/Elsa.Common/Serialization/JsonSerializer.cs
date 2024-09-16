using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.Common.Contracts;

namespace Elsa.Common.Serialization;

/// <summary>
/// Provides JSON serialization services.
/// </summary>
public class StandardJsonSerializer : ConfigurableSerializer, IJsonSerializer
{
    /// <inheritdoc />
    public StandardJsonSerializer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type is not known at compile time.")]
    public string Serialize(object value)
    {
        var options = GetOptions();
        return JsonSerializer.Serialize(value, options);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type is not known at compile time.")]
    public string Serialize(object? value, Type type)
    {
        var options = GetOptions();
        return JsonSerializer.Serialize(value, type, options);
    }

    /// <inheritdoc />
    public string Serialize<T>(T value)
    {
        return Serialize(value, typeof(T));
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type is not known at compile time.")]
    public object Deserialize(string json)
    {
        var options = GetOptions();
        return JsonSerializer.Deserialize<object>(json, options)!;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type is not known at compile time.")]
    public object Deserialize(string json, Type type)
    {
        var options = GetOptions();
        return JsonSerializer.Deserialize(json, type, options)!;
    }

    /// <inheritdoc />
    public T Deserialize<T>(string json)
    {
        return (T)Deserialize(json, typeof(T));
    }
}