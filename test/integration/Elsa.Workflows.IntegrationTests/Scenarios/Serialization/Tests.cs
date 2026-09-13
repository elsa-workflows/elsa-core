using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.Serialization;

public class Tests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowDefinitionPublisher _publisher;
    private readonly IActivitySerializer _serializer;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).WithCapturingTextWriter(_capturingTextWriter).Build();
        _services.GetRequiredService<IWorkflowBuilderFactory>();
        _publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        _serializer = _services.GetRequiredService<IActivitySerializer>();
    }

    [Test]
    [DisplayName("Can serialize newly created workflow definition")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        var workflowDefinition = await _publisher.NewAsync();
        var root = _serializer.Deserialize(workflowDefinition.StringData!);
        await Assert.That(root).IsNotNull();
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
