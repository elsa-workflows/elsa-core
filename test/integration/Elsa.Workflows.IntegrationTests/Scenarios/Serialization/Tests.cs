using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.Serialization;

public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowDefinitionPublisher _publisher;
    private readonly IActivitySerializer _serializer;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _services.GetRequiredService<IWorkflowBuilderFactory>();
        _publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        _serializer = _services.GetRequiredService<IActivitySerializer>();
    }

    [Fact(DisplayName = "Can serialize newly created workflow definition")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        var workflowDefinition = _publisher.New();
        var root = _serializer.Deserialize(workflowDefinition.StringData);
        Assert.NotNull(root);
    }
}