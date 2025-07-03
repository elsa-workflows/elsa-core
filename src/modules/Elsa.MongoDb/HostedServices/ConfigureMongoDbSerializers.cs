using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.MongoDb.Serializers;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson.Serialization.Serializers;
using static Elsa.MongoDb.Helpers.BsonSerializerHelpers;

namespace Elsa.MongoDb.HostedServices;

/// <summary>
/// A hosted service that configures and registers custom MongoDB serializers for various types.
/// </summary>
/// <remarks>
/// This class implements <see cref="IHostedService"/> and is responsible for registering serializers to handle
/// specific types such as <see cref="object"/>, <see cref="Type"/>, <see cref="Variable"/>, <see cref="Version"/>,
/// <see cref="JsonElement"/>, <see cref="JsonNode"/>, and <see cref="FlowScope"/>.
/// It uses helper methods to register these serializers during the application's startup process.
/// </remarks>
public class ConfigureMongoDbSerializers(IPayloadSerializer payloadSerializer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        TryRegisterSerializer(typeof(object), new PolymorphicSerializer());
        TryRegisterSerializer(typeof(Type), new TypeSerializer());
        TryRegisterSerializer(typeof(Variable), new VariableSerializer());
        TryRegisterSerializer(typeof(Version), new VersionSerializer());
        TryRegisterSerializer(typeof(JsonElement), new JsonElementSerializer());
        TryRegisterSerializer(typeof(JsonNode), new JsonNodeBsonConverter());
        TryRegisterSerializer(typeof(FlowScope), new FlowScopeSerializer(payloadSerializer));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}