using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="Variable"/> for configuring storage.
/// </summary>
public static class VariableExtensions
{
    private static JsonSerializerOptions? _serializerOptions;

    private static JsonSerializerOptions SerializerOptions =>
        _serializerOptions ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.Preserve,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        }.WithConverters(
            new JsonStringEnumConverter(),
            new ExpandoObjectConverterFactory());

    /// <summary>
    /// Configures the variable to use the <see cref="WorkflowStorageDriver"/>.
    /// </summary>
    public static Variable WithWorkflowStorage(this Variable variable) => variable.WithStorage<WorkflowStorageDriver>();

    /// <summary>
    /// Configures the variable to use the <see cref="WorkflowStorageDriver"/>.
    /// </summary>
    public static Variable<T> WithWorkflowStorage<T>(this Variable<T> variable) => (Variable<T>)variable.WithStorage<WorkflowStorageDriver>();

    /// <summary>
    /// Configures the variable to use the <see cref="MemoryStorageDriver"/>.
    /// </summary>
    public static Variable WithMemoryStorage(this Variable variable) => variable.WithStorage<MemoryStorageDriver>();

    /// <summary>
    /// Configures the variable to use the <see cref="MemoryStorageDriver"/>.
    /// </summary>
    public static Variable<T> WithMemoryStorage<T>(this Variable<T> variable) => (Variable<T>)variable.WithStorage<MemoryStorageDriver>();

    /// <summary>
    /// Configures the variable to use the specified <see cref="IStorageDriver"/> type.
    /// </summary>
    public static Variable WithStorage<T>(this Variable variable) => variable.WithStorage(typeof(T));

    /// <summary>
    /// Configures the variable to use the specified <see cref="IStorageDriver"/> type.
    /// </summary>
    public static Variable WithStorage(this Variable variable, Type storageDriverType)
    {
        variable.StorageDriverType = storageDriverType;
        return variable;
    }

    /// <summary>
    /// Converts the specified value into a type that is compatible with the variable.
    /// </summary>
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public static object? ParseValue(this Variable variable, object? value)
    {
        var genericType = variable.GetType().GenericTypeArguments.FirstOrDefault();
        var converterOptions = new ObjectConverterOptions(SerializerOptions);
        return genericType == null ? value : value?.ConvertTo(genericType, converterOptions);
    }

    /// <summary>
    /// Return the type of the variable.
    /// </summary>
    public static Type GetVariableType(this Variable variable)
    {
        var variableType = variable.GetType();
        return variableType.GenericTypeArguments.Any() ? variableType.GetGenericArguments().First() : typeof(object);
    }
}