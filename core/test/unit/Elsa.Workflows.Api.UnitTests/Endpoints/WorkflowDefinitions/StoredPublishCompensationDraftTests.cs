using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.UnitTests.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Publish;
using Elsa.Workflows.Api.Security;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.WorkflowDefinitions;

[Collection(nameof(FastEndpointsCollection))]
public class StoredPublishCompensationDraftTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Publish_AlreadyPublishedCompensationPlaceholder_ReturnsDiagnosticWithoutPublishing(bool originalJsonAvailable)
    {
        var opaqueScope = new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName)
        {
            Id = "opaque-scope",
            OriginalActivityJson = originalJsonAvailable ? """
                {"type":"Elsa.CompensationScope","version":3,"draftSchemaVersion":1,"steps":[{"stepId":"step-1"}]}
                """ : "not-json"
        };
        var attempt = await PublishAsync(opaqueScope);
        using var response = attempt.Response;

        Assert.Equal(StatusCodes.Status400BadRequest, attempt.StatusCode);
        Assert.StartsWith("application/json", attempt.ContentType, StringComparison.Ordinal);
        Assert.Equal(CompensationScope.DraftOnlyDiagnosticCode, response.RootElement.GetProperty("code").GetString());
        Assert.Equal("scope", response.RootElement.GetProperty("data").GetProperty("diagnostics")[0].GetProperty("role").GetString());
        Assert.Equal("opaque-scope", response.RootElement.GetProperty("data").GetProperty("diagnostics")[0].GetProperty("scopeActivityId").GetString());
        await attempt.Publisher.DidNotReceive().PublishAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
        await attempt.Linker.DidNotReceive().MapAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_NestedOpaqueCompensationWithUnrelatedMissingTypeName_ReturnsChildDiagnostics()
    {
        const string scopeJson = """
            {"type":"Elsa.CompensationScope","version":3,"steps":[{"stepId":"step-1","forward":{"id":"forward-1"},"handler":{"id":"handler-1"},"mappings":[{"mappingId":"mapping-1"}]}]}
            """;
        var nestedNotFoundJson = JsonSerializer.Serialize(new
        {
            type = ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>(),
            version = 1,
            missingTypeName = "Elsa.IntermediateUnknownActivity",
            originalActivityJson = scopeJson
        });
        var opaqueScope = new NotFoundActivity(missingTypeName: "Elsa.OuterUnknownActivity")
        {
            Id = "opaque-scope",
            OriginalActivityJson = nestedNotFoundJson
        };
        var attempt = await PublishAsync(opaqueScope);
        using var response = attempt.Response;
        var diagnostics = response.RootElement.GetProperty("data").GetProperty("diagnostics").EnumerateArray().ToArray();
        var byRole = diagnostics.ToDictionary(x => x.GetProperty("role").GetString()!);

        Assert.Equal(StatusCodes.Status400BadRequest, attempt.StatusCode);
        Assert.Equal(new[] { "forward", "handler", "mapping", "scope" }, byRole.Keys.Order().ToArray());
        Assert.Equal("opaque-scope", byRole["scope"].GetProperty("scopeActivityId").GetString());
        Assert.Equal("step-1", byRole["forward"].GetProperty("stepId").GetString());
        Assert.Equal("forward-1", byRole["forward"].GetProperty("relatedActivityIds")[0].GetString());
        Assert.Equal("handler-1", byRole["handler"].GetProperty("relatedActivityIds")[0].GetString());
        Assert.Equal("mapping-1", byRole["mapping"].GetProperty("mappingIds")[0].GetString());
        await attempt.Publisher.DidNotReceive().PublishAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
        await attempt.Linker.DidNotReceive().MapAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    private static async Task<PublishAttempt> PublishAsync(NotFoundActivity opaqueScope)
    {
        var definition = new WorkflowDefinition
        {
            Id = "version-1",
            DefinitionId = "definition-1",
            Version = 1,
            IsPublished = true,
            MaterializerName = "Json"
        };
        var store = Substitute.For<IWorkflowDefinitionStore>();
        store.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()).Returns(definition);
        var root = new Sequence { Id = "root" };
        var rootNode = new ActivityNode(root, "root");
        var scopeNode = new ActivityNode(opaqueScope, "scope");
        scopeNode.AddParent(rootNode);
        rootNode.AddChild(scopeNode);
        var graph = new WorkflowGraph(new Workflow(root), rootNode, [rootNode, scopeNode]);
        var workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
        workflowDefinitionService.MaterializeWorkflowAsync(definition, Arg.Any<CancellationToken>()).Returns(graph);
        var publisher = Substitute.For<IWorkflowDefinitionPublisher>();
        var authorizationService = Substitute.For<IAuthorizationService>();
        authorizationService.AuthorizeAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>(), Arg.Any<object>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());
        var linker = Substitute.For<IWorkflowDefinitionLinker>();
        var scriptAuthorization = new WorkflowDefinitionScriptAuthorizationService(
            Substitute.For<IActivityVisitor>(),
            Substitute.For<IExpressionDescriptorRegistry>());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var endpoint = Factory.Create<Publish>(context, store, publisher, linker, authorizationService, workflowDefinitionService, scriptAuthorization);
        await endpoint.HandleAsync(new Request { DefinitionId = definition.DefinitionId }, CancellationToken.None);
        context.Response.Body.Position = 0;
        var response = await JsonDocument.ParseAsync(context.Response.Body);

        return new PublishAttempt(context.Response.StatusCode, context.Response.Headers.ContentType.ToString(), response, publisher, linker);
    }

    private sealed record PublishAttempt(int StatusCode, string ContentType, JsonDocument Response, IWorkflowDefinitionPublisher Publisher, IWorkflowDefinitionLinker Linker);
}
