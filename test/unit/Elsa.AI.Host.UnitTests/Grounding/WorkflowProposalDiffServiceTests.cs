using System.Text.Json.Nodes;
using Elsa.AI.Host.Services;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class WorkflowProposalDiffServiceTests
{
    [Test]
    [DisplayName("Workflow proposal diff compares draft against baseline graph")]
    public async Task WorkflowProposalDiffComparesDraftAgainstBaselineGraph()
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

        await Assert.That(diff.AddedActivityIds).IsEquivalentTo(["added"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(diff.RemovedActivityIds).IsEquivalentTo(["removed"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
}
