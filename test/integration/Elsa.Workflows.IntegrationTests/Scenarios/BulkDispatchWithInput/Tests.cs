using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWithInput;

public class Tests
{
    private readonly IServiceProvider _services;
    private readonly Spy _spy;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .AddWorkflow<ParentWorkflow>()
            .AddWorkflow<ChildWorkflow>()
            .ConfigureServices(services =>
            {
                services.AddSingleton<Spy>();
                services.AddNotificationHandler<TestHandler, WorkflowDefinitionDispatching>();
            })
            .Build();
        
        _spy = _services.GetRequiredService<Spy>();
    }

    [Fact(DisplayName = "Each dispatched child workflow receives its own input dictionary")]
    public async Task BulkDispatch_EachChildReceivesDistinctInputDictionary()
    {
        // Arrange
        await _services.PopulateRegistriesAsync();

        // Act
        await _services.RunWorkflowUntilEndAsync(nameof(ParentWorkflow));

        // Assert - each dispatch should receive a distinct dictionary instance
        Assert.Equal(3, _spy.CapturedInputReferences.Count);
        Assert.Equal(3, _spy.CapturedInputReferences.Distinct().Count());

        // Assert - each dispatch should have its corresponding item value
        var items = _spy.CapturedInputSnapshots
            .Select(s => s?.GetValueOrDefault<string>("Item"))
            .ToList();
        
        Assert.Contains("Apple", items);
        Assert.Contains("Banana", items);
        Assert.Contains("Cherry", items);
    }
}
