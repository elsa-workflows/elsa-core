using System.Collections.ObjectModel;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Serialization.VariableTypes;

public class Tests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IPayloadSerializer _payloadSerializer;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();
        _payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();
    }
    
    [Test]
    [DisplayName("Variable types remain intact after serialization")]
    public async Task Test1()
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
        await Assert.That(deserializedVariable).IsOfType(typeof(Variable<bool>));
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
