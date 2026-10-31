using Elsa.Workflows.Management;
using Xunit;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Support;

/// <summary>
/// Publishes the latest version of a definition through the real <see cref="IWorkflowDefinitionPublisher"/> — the
/// one place every test that needs a published-then-edited definition goes through, rather than each duplicating
/// this call and its success assertion.
/// </summary>
internal static class DefinitionPublishing
{
    /// <summary>
    /// Publishes the latest version of <paramref name="definitionId"/> via <paramref name="publisher"/>, asserting
    /// success so a refusal — e.g. #8078's "Trigger should have a payload" for a none-start BPMN process — fails the
    /// test loudly with the publisher's own validation messages rather than leaving the definition unpublished.
    /// </summary>
    public static async Task PublishLatestAsync(IWorkflowDefinitionPublisher publisher, string definitionId)
    {
        var result = await publisher.PublishAsync(definitionId);
        Assert.True(result.Succeeded, string.Join("; ", result.ValidationErrors.Select(e => e.Message)));
    }
}
