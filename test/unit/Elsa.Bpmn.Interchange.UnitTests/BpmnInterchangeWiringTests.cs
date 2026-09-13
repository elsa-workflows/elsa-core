using Bpmn.Interchange;
using System.Threading.Tasks;

namespace Elsa.Bpmn.Interchange.UnitTests;

public class BpmnInterchangeWiringTests
{
    [Test]
    public async Task BpmnXmlReader_IsReachable()
    {
        var reader = new BpmnXmlReader();

        await Assert.That(reader).IsNotNull();
    }
}