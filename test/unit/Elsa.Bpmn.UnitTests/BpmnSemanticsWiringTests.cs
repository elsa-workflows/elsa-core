using Bpmn.Semantics;
using System.Threading.Tasks;

namespace Elsa.Bpmn.UnitTests;

public class BpmnSemanticsWiringTests
{
    [Test]
    public async Task CreateDefault_ReturnsInterpreter()
    {
        var interpreter = BpmnInterpreter.CreateDefault();

        await Assert.That(interpreter).IsNotNull();
    }
}