using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.LiquidLists;

public sealed class Tests : IDisposable
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(configure => configure.UseHttp())
            .Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact]
    public async Task GetProducts()
    {
        await _services.PopulateRegistriesAsync();
        RunWorkflowResult result = await _workflowRunner.RunAsync(new JavascriptAndLiquidWorkflow());
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);

        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("First product id: 1", lines);
        Assert.Contains("First product price rounded: 13", lines);
        Assert.Contains("First product as json: {\"id\":1,\"price\":12.99}", lines);
        Assert.Contains("Second product id: 2", lines);

        Assert.Contains("Single product id: 2", lines);
        Assert.Contains("Single product as json: {\"id\":2,\"price\":10}", lines);
    }

    public void Dispose()
    {
        _capturingTextWriter.Dispose();
        GC.SuppressFinalize(this);
    }
}