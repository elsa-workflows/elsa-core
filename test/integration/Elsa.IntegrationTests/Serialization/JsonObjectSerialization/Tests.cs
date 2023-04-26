using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Serialization.JsonObjectSerialization;

public class Tests
{
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
    }

    [Fact(DisplayName = "Serialize and deserialize JsonObject")]
    public void Test1()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var transformationJson = File.ReadAllText("Serialization/JsonObjectSerialization/data.json");

        var jsonObject = JsonObject.Parse(transformationJson);

        var jsonString = payloadSerializer.Serialize(jsonObject);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string,object>>(jsonString);

        Assert.Equal("Created", transformationModel["StatusCode"]);


    }
}

