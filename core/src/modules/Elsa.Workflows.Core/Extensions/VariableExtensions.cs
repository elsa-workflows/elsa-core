using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Serialization.Converters;

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
    /// Configures the variable to use the <see cref="WorkflowInstanceStorageDriver"/>.
    /// </summary>
    public static Variable WithWorkflowStorage(this Variable variable) => variable.WithStorage<WorkflowInstanceStorageDriver>();

    /// <summary>
    /// Configures the variable to use the <see cref="WorkflowInstanceStorageDriver"/>.
    /// </summary>
    public static Variable<T> WithWorkflowStorage<T>(this Variable<T> variable) => (Variable<T>)variable.WithStorage<WorkflowInstanceStorageDriver>();

    /// <summary>
    /// Configures the variable to use the <see cref="MemoryStorageDriver"/>.
    /// </summary>
    public static Variable WithMemoryStorage(this Variable variable) => variable.WithStorage<MemoryStorageDriver>();

    /// <summary>
    /// Configures the variable to use the <see cref="MemoryStorageDriver"/>.
    /// </summary>
    public static Variable<T> WithMemoryStorage<T>(this Variable<T> variable) => (Variable<T>)variable.WithStorage<MemoryStorageDriver>();

    extension(Variable variable)
    {
        /// <summary>
        /// Configures the variable to use the specified <see cref="IStorageDriver"/> type.
        /// </summary>
        public Variable WithStorage<T>() => variable.WithStorage(typeof(T));

        /// <summary>
        /// Configures the variable to use the specified <see cref="IStorageDriver"/> type.
        /// </summary>
        public Variable WithStorage(Type storageDriverType)
        {
            variable.StorageDriverType = storageDriverType;
            return variable;
        }

        public void Set(ActivityExecutionContext context, object? value)
        {
            var parsedValue = variable.ParseValue(value);
            // Set the value.
            ((MemoryBlockReference)variable).Set(context, parsedValue);
        }

        /// <summary>
        /// Converts the specified value into a type that is compatible with the variable.
        /// </summary>
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
        public object? ParseValue(object? value)
        {
            var genericType = variable.GetType();
            return ParseValue(genericType, value);
        }
    }

    public static object? ParseValue(Type type, object? value)
    {
        var genericType = type.GenericTypeArguments.FirstOrDefault();
        var converterOptions = new ObjectConverterOptions(SerializerOptions);
        return genericType == null ? value : value?.ConvertTo(genericType, converterOptions);
    }
    
    extension(Variable variable)
    {
        /// <summary>
        /// Converts the specified value into a type that is compatible with the variable.
        /// </summary>
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
        public bool TryParseValue(object? value, out object? parsedValue)
        {
            try
            {
                parsedValue = variable.ParseValue(value);
                return true;
            }
            catch
            {
                parsedValue = null;
                return false;
            }
        }

        /// <summary>
        /// Return the type of the variable.
        /// </summary>
        public Type GetVariableType()
        {
            var variableType = variable.GetType();
            return variableType.GenericTypeArguments.Any() ? variableType.GetGenericArguments().First() : typeof(object);
        }
    }
}