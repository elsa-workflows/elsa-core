using Elsa.AI.Host.Endpoints.Ai.Capabilities;

namespace Elsa.AI.IntegrationTests;

public class AiCapabilitiesEndpointTests
{
    [Fact(DisplayName = "Capabilities endpoint advertises Weaver MVP capabilities")]
    public async Task CapabilitiesEndpointAdvertisesWeaverMvpCapabilities()
    {
        var endpoint = new Endpoint();

        var response = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.True(response.Streaming);
        Assert.True(response.ProposalReview);
        Assert.Contains("WorkflowDefinition", response.SupportedAttachmentKinds);
    }
}
