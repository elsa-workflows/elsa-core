using System.Text.Json.Nodes;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class WorkflowProposalDiffServiceTests
{
    [Fact(DisplayName = "Workflow proposal diff compares draft against baseline graph")]
    public void WorkflowProposalDiffComparesDraftAgainstBaselineGraph()
    {
        var service = new WorkflowProposalDiffService();
        var baseline = new JsonObject
        {
            ["activities"] = new JsonArray
            {
                new JsonObject { ["id"] = "kept", ["type"] = "Elsa.WriteLine" },
                new JsonObject { ["id"] = "removed", ["type"] = "Elsa.Http.HttpEndpoint" }
            }
        };
        var draft = new JsonObject
        {
            ["activities"] = new JsonArray
            {
                new JsonObject { ["id"] = "kept", ["type"] = "Elsa.WriteLine" },
                new JsonObject { ["id"] = "added", ["type"] = "Elsa.SendEmail" }
            }
        };

        var diff = service.CreateDiff(draft, baseline);

        Assert.Equal(["added"], diff.AddedActivityIds);
        Assert.Equal(["removed"], diff.RemovedActivityIds);
    }
}
