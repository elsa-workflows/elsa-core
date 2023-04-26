using System.Text.Json;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="Variable"/> for configuring storage.
/// </summary>
public static class VariableExtensions
{
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
    public static object ParseValue(this Variable variable, object value)
    {
        var genericType = variable.GetType().GenericTypeArguments.FirstOrDefault();
        var jsonSerializerOptions = new JsonSerializerOptions();
        jsonSerializerOptions.Converters.Add(new ExpandoObjectConverterFactory());
        var converterOptions = new ObjectConverterOptions(jsonSerializerOptions);
        return genericType == null ? value : value.ConvertTo(genericType, converterOptions)!;
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