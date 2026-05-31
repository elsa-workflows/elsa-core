using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Logging;
using Elsa.Common.Serialization;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Produces <see cref="VariableConverter"/> instances for <see cref="Variable"/> and <see cref="Variable{T}"/>.
/// </summary>
public class VariableConverterFactory : JsonConverterFactory
{
    private readonly ISerializationTypeRegistry _workflowJsonTypeRegistry;
    private readonly ILoggerFactory _loggerFactory;

    /// <inheritdoc />
    // ReSharper disable once ContextualLoggerProblem
    public VariableConverterFactory(ISerializationTypeRegistry workflowJsonTypeRegistry, ILoggerFactory loggerFactory)
    {
        _workflowJsonTypeRegistry = workflowJsonTypeRegistry;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Variable).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new VariableConverter(_workflowJsonTypeRegistry, _loggerFactory.CreateLogger<VariableMapper>());
}
