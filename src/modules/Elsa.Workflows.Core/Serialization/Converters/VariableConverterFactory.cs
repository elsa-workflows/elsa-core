using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Produces <see cref="VariableConverter"/> instances for <see cref="Variable"/> and <see cref="Variable{T}"/>.
/// </summary>
public class VariableConverterFactory : JsonConverterFactory
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;
    private readonly ILoggerFactory _loggerFactory;

    /// <inheritdoc />
    // ReSharper disable once ContextualLoggerProblem
    public VariableConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry, ILoggerFactory loggerFactory)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Variable).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new VariableConverter(_wellKnownTypeRegistry, _loggerFactory.CreateLogger<VariableMapper>());
}