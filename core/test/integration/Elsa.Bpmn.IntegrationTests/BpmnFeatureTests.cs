using Elsa.Extensions;
using Elsa.Features.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests;

public class BpmnFeatureTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public BpmnFeatureTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Build_WithUseBpmn_RegistersBpmnFeature()
    {
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmn())
            .Build();

        var registry = services.GetRequiredService<IInstalledFeatureRegistry>();

        Assert.NotNull(registry.Find("Elsa.Bpmn"));
    }
}
