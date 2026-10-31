using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Copilot.UnitTests;

public class CopilotBoundaryTests
{
    [Fact(DisplayName = "Abstractions expose provider-neutral contracts")]
    public void AbstractionsExposeProviderNeutralContracts()
    {
        var contractTypes = new[]
        {
            typeof(IAIProvider),
            typeof(IAIOrchestrator),
            typeof(IAITool),
            typeof(IAIContextProvider),
            typeof(IAIProposalStore),
            typeof(AIProviderEvent),
            typeof(AIStreamEvent)
        };

        Assert.All(contractTypes, type => Assert.DoesNotContain("Copilot", type.FullName, StringComparison.OrdinalIgnoreCase));
    }
}
