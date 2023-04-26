using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.IntegrationTests.Serialization.Polymorphism;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.Serialization.ReferenceHandlers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elsa.IntegrationTests.Serialization.JsonObjectSerialization;

public class Tests
{
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
    }

    [Fact(DisplayName = "Objects mixed with primitive, complex, expando objects and arrays of these can be serialized")]
    public void Test1()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var transformationJson = File.ReadAllText("Serialization/JsonObjectSerialization/data.json");



        //var holder = new TransformationHolder { Transformation = jsonObject };

        //var jsonString = payloadSerializer.Serialize(holder);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string,object>>(transformationJson);

        int i = 0;
        
    }

   
   
    private JsonSerializerOptions GetSerializerOptions()
    {
        var referenceHandler = new CrossScopedReferenceHandler();

        var options = new JsonSerializerOptions
        {
            //ReferenceHandler = referenceHandler,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);
        options.Converters.Add(new PolymorphicObjectConverterFactory());
        options.Converters.Add(ActivatorUtilities.CreateInstance<TypeJsonConverter>(_services));
        return options;
    }
}

