using Elsa.AI.Host.Services;
using Elsa.Workflows.Management.Entities;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class WorkflowGroundingMapperTests
{
    [Fact(DisplayName = "Workflow mapper extracts activity types from serialized graph")]
    public void WorkflowMapperExtractsActivityTypesFromSerializedGraph()
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

        Assert.Equal(2, graph.ActivityCount);
        Assert.Contains("Elsa.Http.HttpEndpoint", graph.ActivityTypes);
        Assert.Contains("Elsa.Email.SendEmail", graph.ActivityTypes);
    }
}
