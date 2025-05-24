using Elsa.Testing.Shared;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.LiquidSubWorkflow;

public class LiquidSubWorkflowTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public LiquidSubWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public async Task ShouldBeAbleToUseLiquidExpressionsToReadInputInSubWorkflows()
    {
        // Arrange.
        var services = new TestApplicationBuilder()
            .WithCapturingTextWriter()
            .Build();

        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        
        // Act.
        await workflowRunner.RunAsync<LiquidParentWorkflow>();

        // Assert.
        var capturedOutput = services.GetRequiredService<StringWriter>();
        var output = capturedOutput.ToString();
        _testOutputHelper.WriteLine(output);

        // Verify that the liquid expressions were able to read the input values.
        Assert.Contains("Person: John Doe, Email: john@example.com", output);
        Assert.Contains("Sub workflow result: John Doe - john@example.com", output);
    }
}