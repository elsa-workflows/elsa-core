using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing said type.
/// </summary>
public class VariableConverter : JsonConverter<Variable>
{
    private readonly VariableMapper _mapper;
    private JsonSerializerOptions? _options;

    /// <inheritdoc />
    // ReSharper disable once ContextualLoggerProblem
    public VariableConverter(IWellKnownTypeRegistry wellKnownTypeRegistry, ILogger<VariableMapper> logger)
    {
        _mapper = new VariableMapper(wellKnownTypeRegistry, logger);
    }

    /// <inheritdoc />
    public override Variable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var newOptions = GetClonedOptions(options);
        var model = JsonSerializer.Deserialize<VariableModel>(ref reader, newOptions)!;
        var variable = _mapper.Map(model);

        return variable;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Variable value, JsonSerializerOptions options)
    {
        var model = _mapper.Map(value);
        JsonSerializer.Serialize(writer, model, options);
    }
    
    private JsonSerializerOptions GetClonedOptions(JsonSerializerOptions options)
    {
        if(_options != null)
            return _options;
        
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Add(new JsonPrimitiveToStringConverter());
        _options = newOptions;
        return newOptions;
    }
}