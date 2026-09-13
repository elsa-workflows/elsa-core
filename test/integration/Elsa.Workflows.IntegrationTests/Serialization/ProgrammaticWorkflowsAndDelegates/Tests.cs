using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Serialization.ProgrammaticWorkflowsAndDelegates;

/// <summary>
/// Contains tests for variable expressions serialization.
/// </summary>
public class Tests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IWorkflowBuilder _workflowBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tests"/> class.
    /// </summary>
    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();
        _activitySerializer = _services.GetRequiredService<IActivitySerializer>();
        var workflowBuilderFactory = _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowBuilder = workflowBuilderFactory.CreateBuilder();
    }
    
    /// <summary>
    /// Tests that programmatic workflows with inputs using delegates do not throw exceptions when serialized.
    /// </summary>
    [Test]
    [DisplayName("Programmatic workflows with inputs using delegates do not throw exceptions when serialized")]
    public async Task Test1()
    {
        var workflow = await _workflowBuilder.BuildWorkflowAsync<GreeterWorkflow>();
        var serializedWorkflow = _activitySerializer.Serialize(workflow);
        
        // If it reached here, the test passed
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
