using Bpmn.Interchange;

namespace Elsa.Bpmn.Interchange.UnitTests;

public class BpmnInterchangeWiringTests
{
    [Fact]
    public void BpmnXmlReader_IsReachable()
    {
        var reader = new BpmnXmlReader();

        Assert.NotNull(reader);
    }
}
