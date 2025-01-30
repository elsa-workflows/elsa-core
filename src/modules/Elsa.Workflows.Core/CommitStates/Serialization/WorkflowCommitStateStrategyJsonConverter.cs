using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.CommitStates.Strategies.Workflows;

namespace Elsa.Workflows.CommitStates.Serialization
{
    public class WorkflowCommitStateStrategyJsonConverter : JsonConverter<IWorkflowCommitStrategy>
    {
        public override IWorkflowCommitStrategy? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            // Read the JSON object
            using var document = JsonDocument.ParseValue(ref reader);
            var rootElement = document.RootElement;

            // Extract the type information
            if (!rootElement.TryGetProperty("$type", out var typeProperty))
                return new DefaultWorkflowStrategy();

            var typeName = typeProperty.GetString();
            if (string.IsNullOrEmpty(typeName)) throw new JsonException("The $type property is empty or null");

            // Resolve the type from the type name
            var type = Type.GetType(typeName);
            if (type == null) throw new JsonException($"Could not resolve type: {typeName}");

            // Ensure the type implements the expected interface
            if (!typeof(IWorkflowCommitStrategy).IsAssignableFrom(type)) throw new JsonException($"The type {typeName} does not implement IWorkflowCommitStateStrategy");

            // Deserialize the "value" object to the resolved type
            if (!rootElement.TryGetProperty("value", out var valueProperty)) throw new JsonException("Could not find 'value' property in JSON payload");

            var value = JsonSerializer.Deserialize(valueProperty.GetRawText(), type, options);

            // Ensure the deserialized object is an IWorkflowCommitStateStrategy
            return value as IWorkflowCommitStrategy ?? throw new JsonException($"Deserialized object is not an IWorkflowCommitStateStrategy");
        }

        public override void Write(Utf8JsonWriter writer, IWorkflowCommitStrategy value, JsonSerializerOptions options)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            // Writing the type name for deserialization purposes.
            var type = value.GetType();

            writer.WriteStartObject();

            // Serialize the type name to enable proper deserialization
            writer.WriteString("$type", type.AssemblyQualifiedName);

            // Serialize the object using the default serializer
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value, type, options);

            writer.WriteEndObject();
        }
    }
}