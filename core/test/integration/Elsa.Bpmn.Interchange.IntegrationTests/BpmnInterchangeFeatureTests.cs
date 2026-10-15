using Elsa.Extensions;
using Elsa.Features.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests;

public class BpmnInterchangeFeatureTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public BpmnInterchangeFeatureTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Build_WithUseBpmnInterchange_RegistersBpmnInterchangeFeature()
    {
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .Build();

        var registry = services.GetRequiredService<IInstalledFeatureRegistry>();

        Assert.NotNull(registry.Find("Elsa.Bpmn"));
        Assert.NotNull(registry.Find("Elsa.BpmnInterchange"));
    }
}
