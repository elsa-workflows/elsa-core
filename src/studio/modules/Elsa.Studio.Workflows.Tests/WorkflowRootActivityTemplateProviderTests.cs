using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class WorkflowRootActivityTemplateProviderTests
{
    private readonly DefaultWorkflowRootActivityTemplateProvider _provider = new();
    private readonly TestIdentityGenerator _identityGenerator = new();

    [Fact]
    public void ListReturnsBuiltInTemplates()
    {
        var templates = _provider.List();

        Assert.Collection(
            templates,
            template => Assert.Equal(DefaultWorkflowRootActivityTemplateProvider.FlowchartKey, template.Key),
            template => Assert.Equal(DefaultWorkflowRootActivityTemplateProvider.SequenceKey, template.Key),
            template => Assert.Equal(DefaultWorkflowRootActivityTemplateProvider.StateMachineKey, template.Key));
    }

    [Fact]
    public void GetDefaultReturnsFlowchart()
    {
        var template = _provider.GetDefault();

        Assert.Equal(DefaultWorkflowRootActivityTemplateProvider.FlowchartKey, template.Key);
    }

    [Theory]
    [MemberData(nameof(RootTemplates))]
    public void CreateRootReturnsExpectedRootJson(string templateKey, string expectedName, string[] expectedArrayProperties)
    {
        var template = _provider.Find(templateKey)!;

        var root = template.CreateRoot(_identityGenerator);

        Assert.Equal("activity-1", root["id"]!.GetValue<string>());
        Assert.Equal(templateKey, root["type"]!.GetValue<string>());
        Assert.Equal(1, root["version"]!.GetValue<int>());
        Assert.Equal(expectedName, root["name"]!.GetValue<string>());

        foreach (var propertyName in expectedArrayProperties)
            Assert.Empty(root[propertyName]!.AsArray());
    }

    public static TheoryData<string, string, string[]> RootTemplates => new()
    {
        { DefaultWorkflowRootActivityTemplateProvider.FlowchartKey, "Flowchart1", [] },
        { DefaultWorkflowRootActivityTemplateProvider.SequenceKey, "Sequence1", ["activities"] },
        { DefaultWorkflowRootActivityTemplateProvider.StateMachineKey, "StateMachine1", ["states", "transitions"] }
    };

    private class TestIdentityGenerator : IIdentityGenerator
    {
        public string GenerateId() => "activity-1";
    }
}
