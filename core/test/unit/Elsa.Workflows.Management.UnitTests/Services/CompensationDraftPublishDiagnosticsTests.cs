using System.Text.Json;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class CompensationDraftPublishDiagnosticsTests
{
    [Fact]
    public void Create_ReportsStableScopeStepRoleAndMappingIdsWithoutAuthoredValues()
    {
        var forward = new Sequence { Id = "forward-activity" };
        var handler = new Sequence { Id = "handler-activity" };
        using var mappingDocument = JsonDocument.Parse("""
            {"mappingId":"mapping-1","source":"secret capture value"}
            """);
        var scope = new CompensationScope
        {
            Id = "scope-1",
            Steps =
            [
                new CompensationStep
                {
                    StepId = "step-1",
                    Name = "private row label",
                    Forward = forward,
                    Handler = handler,
                    Mappings = [mappingDocument.RootElement.Clone()]
                }
            ]
        };
        var root = new Sequence { Id = "root" };
        var rootNode = new ActivityNode(root, "root");
        var scopeNode = new ActivityNode(scope, "scope");
        scopeNode.AddParent(rootNode);
        rootNode.AddChild(scopeNode);
        var graph = new WorkflowGraph(new Workflow(root), rootNode, [rootNode, scopeNode]);

        var diagnostics = CompensationDraftPublishDiagnostics.Create(graph);
        var byRole = diagnostics.ToDictionary(GetRole);

        Assert.Equal(new[] { "forward", "handler", "mapping", "scope" }, byRole.Keys.Order().ToArray());
        Assert.All(diagnostics, diagnostic => Assert.Equal(CompensationDraftPublishDiagnostics.Code, diagnostic.Code));
        Assert.Equal("scope-1", GetDataString(byRole["scope"], "scopeActivityId"));
        Assert.Equal("step-1", GetDataString(byRole["forward"], "stepId"));
        Assert.Equal("forward-activity", GetDataArray(byRole["forward"], "relatedActivityIds").Single());
        Assert.Equal("handler-activity", GetDataArray(byRole["handler"], "relatedActivityIds").Single());
        Assert.Equal("mapping-1", GetDataArray(byRole["mapping"], "mappingIds").Single());

        var serialized = JsonSerializer.Serialize(diagnostics);
        Assert.DoesNotContain("private row label", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("secret capture value", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_RefusesOpaqueCompensationTypeEvenWhenOriginalJsonIsUnavailable()
    {
        var root = new Sequence { Id = "root" };
        var rootNode = new ActivityNode(root, "root");
        var opaque = new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName)
        {
            Id = "opaque-scope",
            OriginalActivityJson = "not-json"
        };
        var opaqueNode = new ActivityNode(opaque, "scope");
        opaqueNode.AddParent(rootNode);
        rootNode.AddChild(opaqueNode);
        var graph = new WorkflowGraph(new Workflow(root), rootNode, [rootNode, opaqueNode]);

        var diagnostic = Assert.Single(CompensationDraftPublishDiagnostics.Create(graph));

        Assert.Equal(CompensationScope.DraftOnlyDiagnosticCode, diagnostic.Code);
        Assert.Equal("opaque-scope", diagnostic.ActivityId);
    }

    private static string GetRole(WorkflowValidationError error) => GetDataString(error, "role");

    private static string GetDataString(WorkflowValidationError error, string propertyName)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(error.Data));
        return document.RootElement.GetProperty(propertyName).GetString()!;
    }

    private static string[] GetDataArray(WorkflowValidationError error, string propertyName)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(error.Data));
        return document.RootElement.GetProperty(propertyName).EnumerateArray().Select(x => x.GetString()!).ToArray();
    }
}
