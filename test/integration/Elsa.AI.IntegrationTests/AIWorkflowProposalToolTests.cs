using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.IntegrationTests;

public class AIWorkflowProposalToolTests
{
    [Fact(DisplayName = "Proposal tool writes reviewable proposal only")]
    public async Task ProposalToolWritesReviewableProposalOnly()
    {
        var proposalStore = new CapturingProposalStore();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIProposalStore>(proposalStore);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AIToolEnablementService>().Enable("workflows.proposeCreate");
        var registry = provider.GetRequiredService<IAIToolRegistry>();

        using var tool = await registry.FindAsync("workflows.proposeCreate", new AIToolQuery { ActorId = "user-1" });
        var result = await tool!.ExecuteAsync(new AIToolExecutionContext
        {
            ActorId = "user-1",
            ConversationId = "conversation-1",
            Arguments = new JsonObject
            {
                ["draft"] = new JsonObject
                {
                    ["activities"] = new JsonArray
                    {
                        new JsonObject { ["id"] = "a1", ["type"] = "Elsa.Http.HttpEndpoint" }
                    }
                },
                ["rationale"] = "Create an HTTP-triggered workflow."
            }
        });

        Assert.Equal(AIToolInvocationStatus.Completed, result.Status);
        var proposal = Assert.Single(proposalStore.Proposals);
        Assert.Equal(AIProposalKind.WorkflowCreate, proposal.Kind);
        Assert.Equal("conversation-1", proposal.ConversationId);
        Assert.NotEqual(AIProposalStatus.Applied, proposal.Status);
    }

    private class CapturingProposalStore : IAIProposalStore
    {
        public List<AIProposal> Proposals { get; } = [];

        public ValueTask<AIProposal?> FindAsync(string id, string? tenantId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Proposals.FirstOrDefault(x => x.Id == id && x.TenantId == tenantId));

        public ValueTask SaveAsync(AIProposal proposal, CancellationToken cancellationToken = default)
        {
            Proposals.Add(proposal);
            return ValueTask.CompletedTask;
        }
    }
}
