using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.Serialization.ReferenceHandlers;
using Xunit;

namespace Elsa.IntegrationTests.Serialization.Polymorphism;

public class Tests
{
    [Fact(DisplayName = "Subsequent activity does not get scheduled when previous activity created a bookmark")]
    public void Test1()
    {
        var countryMetadata = new ExpandoObject() as IDictionary<string, object>;
        countryMetadata["Population"] = 17000000;

        var address = new ExpandoObject() as IDictionary<string, object>;
        address["AddressLine1"] = "1 Street";
        address["Country"] = new Country
        {
            Code = "NL",
            Name = "The Netherlands",
            Metadata = countryMetadata
        };
        
        var customer = new ExpandoObject() as IDictionary<string, object>;
        customer["Id"] = "alice-1";
        customer["FirstName"] = "Alice";
        customer["LastName"] = "Smith";
        customer["Email"] = "alice.smith@example.com";
        customer["Address"] = address;
        customer["Order"] = new Order
        {
            Id = "order-1",
            CustomerId = "customer-1",
            Number = 1
        };

        var state = new SomeState
        {
            Payload = customer
        };
        
        var json = JsonSerializer.Serialize(state, GetSerializerOptions());
        var deserializedState = JsonSerializer.Deserialize<SomeState>(json, GetSerializerOptions());
        Assert.Equal(json, json);
    }
    
    private JsonSerializerOptions GetSerializerOptions()
    {
        var referenceHandler = new CrossScopedReferenceHandler();

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = referenceHandler,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter());
        //options.Converters.Add(new PolymorphicJsonConverter());
        //options.Converters.Add(new ComplexObjectWithExpandoPropertiesJsonConverter());
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);
        return options;
    }

    public class SomeState
    {
        [JsonConverter(typeof(PolymorphicJsonConverter))]
        public object Payload { get; set; }
    }

    public class Order
    {
        public string Id { get; set; }
        public long Number { get; set; }
        public string CustomerId { get; set; }
    }
    
    public class Country
    {
        public string Code { get; set; }
        public string Name { get; set; }
        
        [JsonConverter(typeof(PolymorphicJsonConverter))]
        public object Metadata { get; set; }
    }
}