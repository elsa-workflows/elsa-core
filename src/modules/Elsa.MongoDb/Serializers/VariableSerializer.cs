using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDb.Serializers;

/// <summary>
/// Serializes a <see cref="Variable"/>.
/// </summary>
public class VariableSerializer : IBsonSerializer<Variable>
{
    private readonly VariableMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableSerializer"/> class.
    /// </summary>
    public VariableSerializer()
    {
        var wellKnownTypeRegistry = WellKnownTypeRegistry.CreateDefault();
        _mapper = new VariableMapper(wellKnownTypeRegistry, NullLogger<VariableMapper>.Instance);
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