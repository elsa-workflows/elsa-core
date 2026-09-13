using Elsa.AI.Host.Services;
using Elsa.Workflows.Management.Entities;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class WorkflowGroundingMapperTests
{
    [Test]
    [DisplayName("Workflow mapper extracts activity types from serialized graph")]
    public async Task WorkflowMapperExtractsActivityTypesFromSerializedGraph()
    {
        var mapper = new WorkflowGroundingMapper();
        var definition = new WorkflowDefinition
        {
            Id = "version-1",
            DefinitionId = "workflow-1",
            Name = "Order intake",
            Version = 1,
            MaterializerName = "Json",
            StringData = """
                         {
                           "root": {
                             "id": "a1",
                             "type": "Elsa.Http.HttpEndpoint",
                             "activities": [
                               { "id": "a2", "typeName": "Elsa.Email.SendEmail" }
                             ]
                           }
                         }
                         """
        };

        var graph = mapper.GetGraph(definition);

        await Assert.That(graph.ActivityCount).IsEqualTo(2);
        await Assert.That(graph.ActivityTypes).Contains("Elsa.Http.HttpEndpoint");
        await Assert.That(graph.ActivityTypes).Contains("Elsa.Email.SendEmail");
    }
}