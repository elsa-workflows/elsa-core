using System.Text.Json.Nodes;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class RuntimeGroundingMapperTests
{
    [Test]
    [DisplayName("Runtime mapper redacts sensitive workflow state")]
    public async Task RuntimeMapperRedactsSensitiveWorkflowState()
    {
        var formatter = new AIGroundingResultFormatter(MicrosoftOptions.Create(new AIHostOptions()));
        var mapper = new RuntimeGroundingMapper(formatter);
        var instance = new WorkflowInstance
        {
            Id = "instance-1",
            DefinitionId = "workflow-1",
            DefinitionVersionId = "version-1",
            WorkflowState = new WorkflowState
            {
                Input = new Dictionary<string, object> { ["password"] = "secret" },
                Output = new Dictionary<string, object> { ["result"] = "ok" }
            }
        };

        var state = mapper.MapState(instance);

        await Assert.That(state["input"]!.AsObject()["password"]!.GetValue<string>()).IsEqualTo("***");
        await Assert.That(state["output"]!.AsObject()["result"]!.GetValue<string>()).IsEqualTo("ok");
    }
}