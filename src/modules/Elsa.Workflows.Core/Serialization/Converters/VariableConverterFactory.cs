using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Options;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Produces <see cref="VariableConverter"/> instances for <see cref="Variable"/> and <see cref="Variable{T}"/>.
/// </summary>
public class VariableConverterFactory : JsonConverterFactory
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<ExpressionOptions>? _expressionOptions;

    /// <inheritdoc />
    // ReSharper disable once ContextualLoggerProblem
    public VariableConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry, ILoggerFactory loggerFactory)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    // ReSharper disable once ContextualLoggerProblem
    public VariableConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry, ILoggerFactory loggerFactory, IOptions<ExpressionOptions> expressionOptions)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _loggerFactory = loggerFactory;
        _expressionOptions = expressionOptions;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Variable).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var logger = _loggerFactory.CreateLogger<VariableMapper>();
        return _expressionOptions != null ? new VariableConverter(_wellKnownTypeRegistry, logger, _expressionOptions) : new VariableConverter(_wellKnownTypeRegistry, logger);
    }
}
