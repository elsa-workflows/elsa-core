using System.Collections.ObjectModel;
using System.Linq;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Serialization.VariableTypes;

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
        var variable = new Variable<bool>();
        
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