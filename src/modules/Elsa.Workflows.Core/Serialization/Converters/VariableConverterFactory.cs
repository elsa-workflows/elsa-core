using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Serialization.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Produces <see cref="VariableConverter"/> instances for <see cref="Variable"/> and <see cref="Variable{T}"/>.
/// </summary>
public class VariableConverterFactory : JsonConverterFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<WorkflowJsonOptions>? _workflowJsonOptions;

    /// <inheritdoc />
    // ReSharper disable once ContextualLoggerProblem
    public VariableConverterFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    // ReSharper disable once ContextualLoggerProblem
    public VariableConverterFactory(ILoggerFactory loggerFactory, IOptions<WorkflowJsonOptions> workflowJsonOptions)
    {
        _loggerFactory = loggerFactory;
        _workflowJsonOptions = workflowJsonOptions;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Variable).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var logger = _loggerFactory.CreateLogger<VariableMapper>();
        return _workflowJsonOptions != null ? new VariableConverter(logger, _workflowJsonOptions) : new VariableConverter(logger);
    }
}
