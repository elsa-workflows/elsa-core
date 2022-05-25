using System.Text.Json;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Serialization;

public class SerializationOptionsConfigurator : ISerializationOptionsConfigurator
{
    public void Configure(JsonSerializerOptions options)
    {
        // options.GetObjectMappingRegistry().Register<Order>(objectMapping => objectMapping
        //      .AutoMap()
        //      .SetDiscriminator("Order")
        //      .SetDiscriminatorPolicy(DiscriminatorPolicy.Always)
        //      .AddDiscriminatorMapping());
    }
}