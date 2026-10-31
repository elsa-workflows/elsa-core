using System.Reflection;
using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using NSubstitute;
using Xunit;

namespace Elsa.Bpmn.UnitTests;

public class BpmnProcessDescriptorTests
{
    [Fact(DisplayName = "BpmnProcess's descriptor declares Done and Cancelled as its only flow ports")]
    public async Task DescribeActivityAsync_DeclaresDoneAndCancelledAsOnlyFlowPorts()
    {
        var defaultValueResolver = Substitute.For<IPropertyDefaultValueResolver>();
        var propertyUIHandlerResolver = Substitute.For<IPropertyUIHandlerResolver>();

        defaultValueResolver.GetDefaultValue(Arg.Any<PropertyInfo>()).Returns((object?)null);
        propertyUIHandlerResolver
            .GetUIPropertiesAsync(Arg.Any<PropertyInfo>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<IDictionary<string, object>>(new Dictionary<string, object>()));

        var describer = new ActivityDescriber(defaultValueResolver, propertyUIHandlerResolver);

        var descriptor = await describer.DescribeActivityAsync(typeof(BpmnProcess));

        var flowPorts = descriptor.Ports.Where(port => port.Type == PortType.Flow).ToList();

        Assert.Equal(2, flowPorts.Count);
        Assert.Contains(flowPorts, port => port.Name == BpmnInterpreter.DoneOutcomeName);
        Assert.Contains(flowPorts, port => port.Name == BpmnInterpreter.CancelledOutcomeName);
    }
}
