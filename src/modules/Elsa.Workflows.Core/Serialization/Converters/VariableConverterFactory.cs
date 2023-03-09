using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Produces <see cref="VariableConverter"/> instances for <see cref="Variable"/> and <see cref="Variable{T}"/>.
/// </summary>
public class VariableConverterFactory : JsonConverterFactory
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;
    private readonly ILogger<VariableConverter> _logger;

    /// <inheritdoc />
    // ReSharper disable once ContextualLoggerProblem
    public VariableConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry, ILogger<VariableConverter> logger)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _logger = logger;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Variable).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new VariableConverter(_wellKnownTypeRegistry, _logger);
}