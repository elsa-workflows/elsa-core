using System.Collections.ObjectModel;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Serialization.VariableTypes;

public class Tests
{
    private readonly IPayloadSerializer _payloadSerializer;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).Build();
        _payloadSerializer = services.GetRequiredService<IPayloadSerializer>();
    }
    
    [Fact(DisplayName = "Variable types remain intact after serialization")]
    public void Test1()
    {
        // Create collection of variables to serialize.
        var variables = new Collection<Variable>();
        var model = new VariablesContainer(variables);
        
        // Create a typed variable.
        var variable = new Variable<bool>("Variable", false);
        
        // Add variable to collection.
        variables.Add(variable);
        
        // Serialize collection.
        var json = _payloadSerializer.Serialize(model);
        
        // Deserialize collection.
        var deserializedModel = _payloadSerializer.Deserialize<VariablesContainer>(json);
        
        // Get the first variable.
        var deserializedVariable = deserializedModel.Variables.First();
        
        // Assert that the variable is of the correct type.
        Assert.IsType<Variable<bool>>(deserializedVariable);
    }
}