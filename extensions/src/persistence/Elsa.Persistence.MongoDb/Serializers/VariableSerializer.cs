using Elsa.Common.Serialization;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Elsa.Persistence.MongoDb.Serializers;

/// <summary>
/// Serializes a <see cref="Variable"/>.
/// </summary>
public class VariableSerializer : IBsonSerializer<Variable>
{
    private readonly VariableMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableSerializer"/> class.
    /// </summary>
    /// <param name="serializationTypeRegistry">The DI-registered serialization type registry used to resolve storage drivers and variable types.</param>
    /// <param name="logger">The logger used by <see cref="VariableMapper"/> when type resolution fails.</param>
    public VariableSerializer(ISerializationTypeRegistry serializationTypeRegistry, ILogger<VariableMapper>? logger = null)
    {
        _mapper = new VariableMapper(serializationTypeRegistry, logger ?? NullLogger<VariableMapper>.Instance);
    }
    
    /// <inheritdoc />
    public Type ValueType => typeof(Variable);

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (Variable)value);
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);

    /// <inheritdoc />
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Variable value)
    {
        if (value == null!)
            context.Writer.WriteNull();
        else
        {
            var model = _mapper.Map(value);
            var serializer = BsonSerializer.LookupSerializer(typeof(VariableModel));
            serializer.Serialize(context, model);
        }
    }

    /// <inheritdoc />
    public Variable Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var bsonType = reader.GetCurrentBsonType();
        
        if (bsonType == BsonType.Null)
        {
            reader.ReadNull();
            return null!;
        }
        
        var serializer = BsonSerializer.LookupSerializer(typeof(VariableModel));
        var model = (VariableModel)serializer.Deserialize(context);
        return _mapper.Map(model);
    }
}