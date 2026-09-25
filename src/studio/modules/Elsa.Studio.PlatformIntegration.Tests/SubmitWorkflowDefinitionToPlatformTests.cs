using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.PlatformIntegration.Contracts;
using Elsa.Studio.PlatformIntegration.Handlers;
using Elsa.Studio.Workflows.Domain.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Elsa.Studio.PlatformIntegration.Tests;

public sealed class SubmitWorkflowDefinitionToPlatformTests
{
    [Fact]
    public async Task Skips_submission_when_platform_options_are_not_configured()
    {
        var service = new FakeSubmissionService();
        var handler = new SubmitWorkflowDefinitionToPlatform(
            Microsoft.Extensions.Options.Options.Create(new PlatformSubmitOptions()),
            service,
            NullLogger<SubmitWorkflowDefinitionToPlatform>.Instance);

        await handler.HandleAsync(new WorkflowDefinitionPublished(WorkflowDefinition()), CancellationToken.None);

        Assert.Equal(0, service.SubmitCount);
    }

    [Fact]
    public async Task Submits_workflow_when_publish_notification_is_received()
    {
        var service = new FakeSubmissionService();
        var handler = new SubmitWorkflowDefinitionToPlatform(
            Microsoft.Extensions.Options.Options.Create(ConfiguredOptions()),
            service,
            NullLogger<SubmitWorkflowDefinitionToPlatform>.Instance);

        await handler.HandleAsync(new WorkflowDefinitionPublished(WorkflowDefinition()), CancellationToken.None);

        Assert.Equal(1, service.SubmitCount);
        Assert.Equal("payment-retry", service.LastWorkflowDefinition?.DefinitionId);
    }

    [Fact]
    public async Task Contains_submission_failures_so_publish_remains_complete()
    {
        var service = new FakeSubmissionService { Exception = new InvalidOperationException("Platform unavailable") };
        var handler = new SubmitWorkflowDefinitionToPlatform(
            Microsoft.Extensions.Options.Options.Create(ConfiguredOptions()),
            service,
            NullLogger<SubmitWorkflowDefinitionToPlatform>.Instance);

        await handler.HandleAsync(new WorkflowDefinitionPublished(WorkflowDefinition()), CancellationToken.None);

        Assert.Equal(1, service.SubmitCount);
    }

    private static PlatformSubmitOptions ConfiguredOptions() =>
        new()
        {
            PlatformEndpoint = new Uri("https://platform.example.test"),
            WorkspaceId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            ApplicationId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            EnvironmentId = Guid.Parse("40000000-0000-0000-0000-000000000001")
        };

    private static WorkflowDefinition WorkflowDefinition() =>
        new()
        {
            Id = "workflow-version-42",
            DefinitionId = "payment-retry",
            Name = "Payment Retry",
            Version = 42,
            Root = new JsonObject
            {
                ["type"] = "Elsa.Flowchart",
                ["version"] = 1
            }
        };

    private sealed class FakeSubmissionService : IPlatformWorkflowSubmissionService
    {
        public int SubmitCount { get; private set; }

        public WorkflowDefinition? LastWorkflowDefinition { get; private set; }

        public Exception? Exception { get; init; }

        public Task<PlatformSubmitResult> SubmitAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            SubmitCount++;
            LastWorkflowDefinition = workflowDefinition;

            if (Exception is not null)
                throw Exception;

            return Task.FromResult(new PlatformSubmitResult(PlatformSubmitStatus.Submitted, "Submitted to Platform.", "artifact-1"));
        }
    }
}
