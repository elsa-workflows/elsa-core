using System.Text.Json;
using Dahomey.Json;
using Dahomey.Json.Attributes;
using Elsa.Contracts;
using Elsa.Samples.Web1.Models;

namespace Elsa.Samples.Web1.Serialization;

public class SerializationOptionsConfigurator : ISerializationOptionsConfigurator
{
    public void Configure(JsonSerializerOptions options)
    {
        options.GetObjectMappingRegistry().Register<Order>(objectMapping => objectMapping
             .AutoMap()
             .SetDiscriminator("Order")
             .SetDiscriminatorPolicy(DiscriminatorPolicy.Always)
             .AddDiscriminatorMapping());
    }
}