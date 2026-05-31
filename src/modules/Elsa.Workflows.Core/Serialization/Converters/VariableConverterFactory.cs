using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Produces <see cref="VariableConverter"/> instances for <see cref="Variable"/> and <see cref="Variable{T}"/>.
/// </summary>
public class VariableConverterFactory : JsonConverterFactory
{
    private readonly IWorkflowJsonTypeRegistry _workflowJsonTypeRegistry;
    private readonly ILoggerFactory _loggerFactory;

    /// <inheritdoc />
    // ReSharper disable once ContextualLoggerProblem
    public VariableConverterFactory(IWorkflowJsonTypeRegistry workflowJsonTypeRegistry, ILoggerFactory loggerFactory)
    {
        _workflowJsonTypeRegistry = workflowJsonTypeRegistry;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Variable).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new VariableConverter(_workflowJsonTypeRegistry, _loggerFactory.CreateLogger<VariableMapper>());
}
