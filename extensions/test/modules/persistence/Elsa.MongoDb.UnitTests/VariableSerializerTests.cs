using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Persistence.MongoDb.Serializers;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Services;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Elsa.MongoDb.UnitTests;

public class VariableSerializerTests
{
    [Fact(DisplayName = "VariableSerializer preserves StorageDriverType when using the populated serialization type registry")]
    public void Serialize_WithPopulatedRegistry_RoundTripsWorkflowInstanceStorageDriver()
    {
        var serializer = CreateSerializer(CreateWorkflowsRegistry());
        var variable = CreateDialogIdVariable();

        var restored = RoundTrip(serializer, variable);

        Assert.Equal(variable.Id, restored.Id);
        Assert.Equal(variable.Name, restored.Name);
        Assert.Equal("abc", restored.Value);
        Assert.Equal(typeof(WorkflowInstanceStorageDriver), restored.StorageDriverType);
        Assert.IsType<Variable<string>>(restored);
    }

    [Fact(DisplayName = "VariableSerializer resolves the 3.7 storageDriverTypeName assembly-qualified name through the populated registry")]
    public void Deserialize_WithLegacyWorkflowInstanceStorageDriverTypeName_RestoresStorageDriverType()
    {
        var serializer = CreateSerializer(CreateWorkflowsRegistry());
        var legacyTypeName = typeof(WorkflowInstanceStorageDriver).GetSimpleAssemblyQualifiedName();
        var model = new VariableModel("dialogIdVariable", "DialogId", "String", "abc", legacyTypeName);

        var restored = DeserializeModel(serializer, model);

        Assert.Equal(typeof(WorkflowInstanceStorageDriver), restored.StorageDriverType);
        Assert.Equal("DialogId", restored.Name);
        Assert.Equal("abc", restored.Value);
    }

    [Fact(DisplayName = "VariableSerializer loses StorageDriverType against an empty default registry (the 3.8.0 regression)")]
    public void Serialize_WithEmptyDefaultRegistry_DropsStorageDriverType()
    {
        var serializer = CreateSerializer(SerializationTypeRegistry.CreateDefault());
        var variable = CreateDialogIdVariable();

        var restored = RoundTrip(serializer, variable);

        Assert.Null(restored.StorageDriverType);
    }

    [Fact(DisplayName = "VariableSerializer serializes null variables as BSON null")]
    public void Serialize_NullVariable_RoundTripsAsNull()
    {
        var serializer = CreateSerializer(CreateWorkflowsRegistry());

        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
        {
            writer.WriteStartDocument();
            writer.WriteName("value");
            serializer.Serialize(BsonSerializationContext.CreateRoot(writer), null!);
            writer.WriteEndDocument();
        }

        using var reader = new BsonDocumentReader(document);
        reader.ReadStartDocument();
        reader.ReadName("value");
        var restored = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));

        Assert.Null(restored);
    }

    private static VariableSerializer CreateSerializer(ISerializationTypeRegistry registry) => new(registry);

    private static ISerializationTypeRegistry CreateWorkflowsRegistry()
    {
        var options = new SerializationTypeOptions();
        options.AddTypeAliasWithLegacyName<WorkflowInstanceStorageDriver>(nameof(WorkflowInstanceStorageDriver));
        options.AddTypeAliasWithLegacyName<MemoryStorageDriver>(nameof(MemoryStorageDriver));
        return new SerializationTypeRegistry(Microsoft.Extensions.Options.Options.Create(options));
    }

    private static Variable<string> CreateDialogIdVariable() =>
        new("DialogId", "abc", "dialogIdVariable")
        {
            StorageDriverType = typeof(WorkflowInstanceStorageDriver)
        };

    private static Variable RoundTrip(VariableSerializer serializer, Variable variable)
    {
        var document = new BsonDocument();
        using (var writer = new BsonDocumentWriter(document))
            serializer.Serialize(BsonSerializationContext.CreateRoot(writer), variable);

        using var reader = new BsonDocumentReader(document);
        return serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }

    private static Variable DeserializeModel(VariableSerializer serializer, VariableModel model)
    {
        var document = model.ToBsonDocument();
        using var reader = new BsonDocumentReader(document);
        return serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }
}
