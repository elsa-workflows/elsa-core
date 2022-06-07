using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Builders;
using Elsa.Workflows.Core.Implementations;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.PersistentVariables;

public class WorkflowInstancePersistenceTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public WorkflowInstancePersistenceTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();

        services.ConfigureDefaultWorkflowExecutionPipeline(pipeline => pipeline
            .UsePersistence()
            .UseStackBasedActivityScheduler());
    }

    [Fact(DisplayName = "Persistent variables are persisted after workflow gets blocked")]
    public async Task Test1()
    {
        // Build the workflow.
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync<BlockingWorkflow>();

        // Run the workflow.
        var result = await _workflowRunner.RunAsync(workflow);

        // Assert the variable was persisted.
        var persistentVariables = result.WorkflowState.PersistentVariables;
        var currentLanguageVariable = persistentVariables.FirstOrDefault();

        Assert.NotNull(currentLanguageVariable);
    }

    [Fact(DisplayName = "Persistent variables are applied before workflow gets resumed")]
    public async Task Test2()
    {
        // Build the workflow.
        var languages = new[] { "C#", "JavaScript", "Haskell" };
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync(new BlockingWorkflow(languages));
        var currentIndex = 0;

        var bookmark = default(Bookmark?);
        var workflowState = default(WorkflowState?);
        var variable = workflow.Variables.First();

        // Continuously resume the workflow until no more bookmarks are returned.
        while (true)
        {
            // Run/resume the workflow.
            var result = workflowState == null 
                ? await _workflowRunner.RunAsync(workflow) 
                : await _workflowRunner.RunAsync(workflow, workflowState, bookmark);

            bookmark = result.Bookmarks.FirstOrDefault();

            if (bookmark == null)
                break;
            
            workflowState = result.WorkflowState;
            
            // Assert that the expected variable is persisted.
            var persistentVariablesDictionary = (IDictionary<string, object>)workflowState.Properties[WorkflowStateStorageDriver.VariablesDictionaryStateKey];
            var stateId = $"{workflowState.Id}:{variable.Name}";
            var persistedValue = persistentVariablesDictionary[stateId];
            var expectedValue = languages[currentIndex];
            
            Assert.Equal(expectedValue, persistedValue);

            currentIndex ++;
        }
    }
}