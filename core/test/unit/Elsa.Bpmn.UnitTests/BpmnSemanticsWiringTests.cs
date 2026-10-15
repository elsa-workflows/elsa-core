using Bpmn.Semantics;

namespace Elsa.Bpmn.UnitTests;

public class BpmnSemanticsWiringTests
{
    [Fact]
    public void CreateDefault_ReturnsInterpreter()
    {
        var interpreter = BpmnInterpreter.CreateDefault();

        Assert.NotNull(interpreter);
    }
}
