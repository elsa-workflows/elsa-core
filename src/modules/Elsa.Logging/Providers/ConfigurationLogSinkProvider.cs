using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Logging.Contracts;
using Elsa.Logging.Models;
using Elsa.Logging.SinkOptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Logging.Providers;

public class ConfigurationLogSinkProvider(IConfiguration configuration, IServiceProvider serviceProvider) : ILogSinkProvider
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new NullableBoolConverter(),
            new JsonStringEnumConverter()
        }
    };

    public Task<IEnumerable<ILogSink>> GetLogSinksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetLogSinks());
    }

    private IEnumerable<ILogSink> GetLogSinks()
    {
        var json = configuration.GetSectionAsJson("LoggingFramework:Sinks");
        var specs = JsonSerializer.Deserialize<List<SinkEnvelope>>(json, _jsonSerializerOptions)!;
        var consoleFactory = serviceProvider.GetRequiredService<ILogSinkFactory<ConsoleSinkOptions>>();
        var fileFactory = serviceProvider.GetRequiredService<ILogSinkFactory<SerilogFileSinkOptions>>();
        var builtSinks = new List<ILogSink>();

        foreach (var spec in specs)
        {
            switch (spec.Type.ToLowerInvariant())
            {
                case "console":
                    var consoleSinkOptions = spec.Options.Deserialize<ConsoleSinkOptions>(_jsonSerializerOptions) ?? new ConsoleSinkOptions();
                    builtSinks.Add(consoleFactory.Create(spec.Name, consoleSinkOptions, serviceProvider));
                    break;
                case "serilogfile":
                    var serilogSinkOptions = spec.Options.Deserialize<SerilogFileSinkOptions>(_jsonSerializerOptions) ?? new SerilogFileSinkOptions();
                    builtSinks.Add(fileFactory.Create(spec.Name, serilogSinkOptions, serviceProvider));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown target type '{spec.Type}'.");
            }
        }

        return builtSinks;
    }
}

public class NullableBoolConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle real boolean
        if (reader.TokenType == JsonTokenType.True)
            return true;
        if (reader.TokenType == JsonTokenType.False)
            return false;
        // Handle string "true"/"false"
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (bool.TryParse(value, out var b))
                return b;
        }

        // Handle null
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        throw new JsonException($"Cannot convert {reader.TokenType} to bool?");
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteBooleanValue(value.Value);
        else
            writer.WriteNullValue();
    }
}