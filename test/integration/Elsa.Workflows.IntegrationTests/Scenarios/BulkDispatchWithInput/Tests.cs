using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWithInput;

public class Tests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly Spy _spy;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
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

    [Test]
    [DisplayName("Each dispatched child workflow receives its own input dictionary")]
    public async Task BulkDispatch_EachChildReceivesDistinctInputDictionary()
    {
        // Arrange
        await _services.PopulateRegistriesAsync();

        // Act
        await _services.RunWorkflowUntilEndAsync(nameof(ParentWorkflow));

        // Assert - each dispatch should receive a distinct dictionary instance
        await Assert.That(_spy.CapturedInputReferences.Count).IsEqualTo(3);
        await Assert.That(_spy.CapturedInputReferences.Distinct().Count()).IsEqualTo(3);

        // Assert - each dispatch should have its corresponding item value
        var items = _spy.CapturedInputSnapshots
            .Select(s => s?.GetValueOrDefault<string>("Item"))
            .ToList();
        
        await Assert.That(items).Contains("Apple");
        await Assert.That(items).Contains("Banana");
        await Assert.That(items).Contains("Cherry");
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
