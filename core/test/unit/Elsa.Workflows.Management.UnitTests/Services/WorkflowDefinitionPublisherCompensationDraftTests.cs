using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowDefinitionPublisherCompensationDraftTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MaterializingStoredDefinitionPreservesOpaqueChildPayloadInNestedCompensationScope(bool scopeUsesItemsWrapper)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddElsa(elsa => elsa.UseWorkflowManagement(management =>
        {
            management.AddActivity<Sequence>();
            management.AddActivity<WriteLine>();
            management.AddActivity<CompensationScope>();
            management.UseFailOnValidationErrors(false);
        }));
        await using var serviceProvider = services.BuildServiceProvider();
        await using var serviceScope = serviceProvider.CreateAsyncScope();
        var scopedServices = serviceScope.ServiceProvider;
        var activityRegistry = scopedServices.GetRequiredService<IActivityRegistry>();
        await activityRegistry.RegisterAsync<Sequence>();
        await activityRegistry.RegisterAsync<WriteLine>();
        await activityRegistry.RegisterAsync<CompensationScope>();
        activityRegistry.Register(new ActivityDescriptor
        {
            TypeName = "Test.OpaquePayloadActivity",
            ClrType = typeof(OpaquePayloadActivity),
            Version = 1,
            Constructor = context => context.CreateActivity<OpaquePayloadActivity>()
        });

        var serializer = scopedServices.GetRequiredService<IActivitySerializer>();
        using var futurePayload = JsonDocument.Parse("""{"_type":"FuturePayload","items":[{"_type":"FutureItem","values":[1,2]}]}""");
        using var ordinaryMetadata = JsonDocument.Parse("""{"_type":"LegacyMetadata","value":7}""");
        using var scopeItems = JsonDocument.Parse("""[{"_type":"FutureScopeItem","values":[3,4]}]""");
        using var scopeValues = JsonDocument.Parse("""[{"_type":"FutureScopeValue","items":[5,6]}]""");
        var scopeExtensionData = new Dictionary<string, JsonElement>
        {
            ["_type"] = JsonSerializer.SerializeToElement("FutureScopeExtension"),
            [scopeUsesItemsWrapper ? "items" : "values"] = (scopeUsesItemsWrapper ? scopeItems : scopeValues).RootElement.Clone()
        };
        var opaqueScope = new CompensationScope
        {
            Id = "opaque-scope",
            AdditionalProperties = scopeExtensionData,
            Steps =
            [
                new CompensationStep
                {
                    StepId = "opaque-step",
                    Forward = new OpaquePayloadActivity
                    {
                        Id = "future-child",
                        Type = "Test.OpaquePayloadActivity",
                        Version = 1,
                        AdditionalProperties = new Dictionary<string, JsonElement>
                        {
                            ["opaque"] = futurePayload.RootElement.Clone()
                        }
                    }
                }
            ]
        };
        var validScope = new CompensationScope
        {
            Id = "valid-scope",
            Steps = [new CompensationStep { StepId = "valid-step", Forward = new WriteLine("hello") { Id = "valid-child" } }]
        };
        var ordinaryPayloadActivity = new OpaquePayloadActivity
        {
            Id = "ordinary-payload",
            Type = "Test.OpaquePayloadActivity",
            Version = 1,
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                ["opaque"] = ordinaryMetadata.RootElement.Clone()
            }
        };
        var sourceJson = serializer.Serialize(new Sequence { Id = "root", Activities = [validScope, opaqueScope, ordinaryPayloadActivity] });
        using var sourceDocument = JsonDocument.Parse(sourceJson);
        var expectedOpaqueScopeJson = sourceDocument.RootElement.GetProperty("activities")[1].GetRawText();
        var workflowDefinitionService = scopedServices.GetRequiredService<IWorkflowDefinitionService>();
        var storedData = sourceJson;

        for (var roundtrip = 0; roundtrip < 2; roundtrip++)
        {
            var definition = new WorkflowDefinition
            {
                Id = "version-1",
                DefinitionId = "definition-1",
                Version = 1,
                MaterializerName = JsonWorkflowMaterializer.MaterializerName,
                StringData = storedData
            };
            var graph = await workflowDefinitionService.MaterializeWorkflowAsync(definition);
            var root = Assert.IsType<Sequence>(graph.Workflow.Root);
            Assert.IsType<CompensationScope>(root.Activities.First());
            var opaque = Assert.IsType<NotFoundActivity>(root.Activities.ElementAt(1));
            var ordinaryPayload = Assert.IsType<OpaquePayloadActivity>(root.Activities.ElementAt(2));
            var ordinaryValue = ordinaryPayload.AdditionalProperties!["opaque"];
            Assert.Equal(JsonValueKind.Object, ordinaryValue.ValueKind);
            Assert.False(ordinaryValue.TryGetProperty("_type", out _));
            Assert.Equal(7, ordinaryValue.GetProperty("value").GetInt32());
            Assert.Equal(CompensationScope.ActivityTypeName, opaque.MissingTypeName);
            Assert.Equal(expectedOpaqueScopeJson, opaque.OriginalActivityJson);

            using var preservedScope = JsonDocument.Parse(opaque.OriginalActivityJson);
            Assert.Equal("FutureScopeExtension", preservedScope.RootElement.GetProperty("_type").GetString());
            var wrapperItems = preservedScope.RootElement.GetProperty(scopeUsesItemsWrapper ? "items" : "values");
            Assert.Equal(scopeUsesItemsWrapper ? "FutureScopeItem" : "FutureScopeValue", wrapperItems[0].GetProperty("_type").GetString());
            Assert.Equal(2, (scopeUsesItemsWrapper
                ? wrapperItems[0].GetProperty("values")
                : wrapperItems[0].GetProperty("items")).GetArrayLength());
            var payload = preservedScope.RootElement.GetProperty("steps")[0].GetProperty("forward").GetProperty("opaque");
            Assert.Equal("FuturePayload", payload.GetProperty("_type").GetString());
            Assert.Equal(2, payload.GetProperty("items")[0].GetProperty("values").GetArrayLength());

            storedData = serializer.Serialize(root);
        }
    }

    [Fact]
    public async Task Publish_RefusesNestedOpaqueCompensationEvenWhenValidationErrorsAreIgnored()
    {
        const string scopeJson = """
            {"type":"Elsa.CompensationScope","version":3,"steps":[{"stepId":"step-1","forward":{"id":"forward-1"},"handler":{"id":"handler-1"},"mappings":[{"mappingId":"mapping-1"}]}]}
            """;
        var innerNotFoundJson = JsonSerializer.Serialize(new
        {
            type = ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>(),
            version = 1,
            missingTypeName = "Elsa.IntermediateUnknownActivity",
            originalActivityJson = scopeJson
        });
        var opaque = new NotFoundActivity(missingTypeName: "Elsa.OuterUnknownActivity")
        {
            Id = "opaque-scope",
            OriginalActivityJson = innerNotFoundJson
        };
        var root = new Sequence { Id = "root" };
        var rootNode = new ActivityNode(root, "root");
        var opaqueNode = new ActivityNode(opaque, "opaque-scope");
        opaqueNode.AddParent(rootNode);
        rootNode.AddChild(opaqueNode);
        var graph = new WorkflowGraph(new Workflow(root), rootNode, [rootNode, opaqueNode]);
        var definition = new WorkflowDefinition { Id = "version-1", DefinitionId = "definition-1", Version = 1 };
        var workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
        workflowDefinitionService.MaterializeWorkflowAsync(definition, Arg.Any<CancellationToken>()).Returns(graph);
        var workflowValidator = Substitute.For<IWorkflowValidator>();
        var publisher = new WorkflowDefinitionPublisher(
            workflowDefinitionService,
            Substitute.For<IWorkflowDefinitionStore>(),
            workflowValidator,
            Substitute.For<IIdentityGenerator>(),
            Substitute.For<IActivitySerializer>(),
            Substitute.For<IMediator>(),
            Substitute.For<ISystemClock>(),
            Microsoft.Extensions.Options.Options.Create(new ManagementOptions { FailOnValidationErrors = false }));

        var result = await publisher.PublishAsync(definition);

        Assert.False(result.Succeeded);
        Assert.Equal(4, result.ValidationErrors.Count);
        Assert.All(result.ValidationErrors, error => Assert.Equal(CompensationScope.DraftOnlyDiagnosticCode, error.Code));
        var forwardError = Assert.Single(result.ValidationErrors, error => CompensationDraftTestData.GetDataString(error, "role") == "forward");
        Assert.Equal(new[] { "forward-1" }, CompensationDraftTestData.GetDataArray(forwardError, "relatedActivityIds"));
        var handlerError = Assert.Single(result.ValidationErrors, error => CompensationDraftTestData.GetDataString(error, "role") == "handler");
        Assert.Equal(new[] { "handler-1" }, CompensationDraftTestData.GetDataArray(handlerError, "relatedActivityIds"));
        var mappingError = Assert.Single(result.ValidationErrors, error => CompensationDraftTestData.GetDataString(error, "role") == "mapping");
        Assert.Equal(new[] { "mapping-1" }, CompensationDraftTestData.GetDataArray(mappingError, "mappingIds"));
        await workflowValidator.DidNotReceiveWithAnyArgs().ValidateAsync(default!, default);
    }

    [Fact]
    public async Task Publish_RefusesNestedOpaqueCompensationAfterRealSerializationAndMaterialization()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddElsa(elsa => elsa.UseWorkflowManagement(management =>
        {
            management.AddActivity<Sequence>();
            management.AddActivity<CompensationScope>();
            management.UseFailOnValidationErrors(false);
        }));
        await using var serviceProvider = services.BuildServiceProvider();
        await using var serviceScope = serviceProvider.CreateAsyncScope();
        var scopedServices = serviceScope.ServiceProvider;
        var activityRegistry = scopedServices.GetRequiredService<IActivityRegistry>();
        await activityRegistry.RegisterAsync<Sequence>();
        await activityRegistry.RegisterAsync<NotFoundActivity>();
        await activityRegistry.RegisterAsync<CompensationScope>();

        var serializer = scopedServices.GetRequiredService<IActivitySerializer>();
        using var mappingDocument = JsonDocument.Parse("""{"mappingId":"mapping-1"}""");
        var scope = new CompensationScope
        {
            Id = "native-scope",
            Version = 3,
            Steps =
            [
                new CompensationStep
                {
                    StepId = "step-1",
                    Forward = new Sequence { Id = "forward-1" },
                    Handler = new Sequence { Id = "handler-1" },
                    Mappings = [mappingDocument.RootElement.Clone()]
                }
            ]
        };
        var unavailableScopeJson = serializer.Serialize(scope);
        var inner = new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName)
        {
            Id = "inner-placeholder",
            OriginalActivityJson = unavailableScopeJson
        };
        var middle = new NotFoundActivity(missingTypeName: "Elsa.MiddleUnknownActivity")
        {
            Id = "middle-placeholder",
            OriginalActivityJson = serializer.Serialize(inner)
        };
        var outer = new NotFoundActivity(missingTypeName: "Elsa.OuterUnknownActivity")
        {
            Id = "outer-placeholder",
            OriginalActivityJson = serializer.Serialize(middle)
        };
        var definition = new WorkflowDefinition
        {
            Id = "version-1",
            DefinitionId = "definition-1",
            Version = 1,
            MaterializerName = JsonWorkflowMaterializer.MaterializerName,
            StringData = serializer.Serialize(new Sequence { Id = "root", Activities = [outer] })
        };

        var workflowDefinitionService = scopedServices.GetRequiredService<IWorkflowDefinitionService>();
        var graph = await workflowDefinitionService.MaterializeWorkflowAsync(definition);
        var materializedPlaceholder = Assert.Single(graph.Nodes.Select(node => node.Activity).OfType<NotFoundActivity>());
        Assert.Equal("Elsa.MiddleUnknownActivity", materializedPlaceholder.MissingTypeName);
        using (var nested = JsonDocument.Parse(materializedPlaceholder.OriginalActivityJson))
        {
            Assert.Equal(ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>(), nested.RootElement.GetProperty("type").GetString());
            Assert.Equal(CompensationScope.ActivityTypeName, nested.RootElement.GetProperty("missingTypeName").GetString());
            using var rawScope = JsonDocument.Parse(nested.RootElement.GetProperty("originalActivityJson").GetString()!);
            Assert.Equal(CompensationScope.ActivityTypeName, rawScope.RootElement.GetProperty("type").GetString());
        }

        var result = await scopedServices.GetRequiredService<IWorkflowDefinitionPublisher>().PublishAsync(definition);

        Assert.False(result.Succeeded);
        Assert.Equal(4, result.ValidationErrors.Count);
        Assert.All(result.ValidationErrors, error => Assert.Equal(CompensationScope.DraftOnlyDiagnosticCode, error.Code));
        Assert.All(result.ValidationErrors, error => Assert.Equal("middle-placeholder", CompensationDraftTestData.GetDataString(error, "scopeActivityId")));
        Assert.Equal(new[] { "forward-1" }, CompensationDraftTestData.GetDataArray(Assert.Single(result.ValidationErrors, error => CompensationDraftTestData.GetDataString(error, "role") == "forward"), "relatedActivityIds"));
        Assert.Equal(new[] { "handler-1" }, CompensationDraftTestData.GetDataArray(Assert.Single(result.ValidationErrors, error => CompensationDraftTestData.GetDataString(error, "role") == "handler"), "relatedActivityIds"));
        Assert.Equal(new[] { "mapping-1" }, CompensationDraftTestData.GetDataArray(Assert.Single(result.ValidationErrors, error => CompensationDraftTestData.GetDataString(error, "role") == "mapping"), "mappingIds"));
    }

    [Fact]
    public async Task Publish_RefusesTypeAliasedOpaqueCompensationInTheMaterializedWholeGraph()
    {
        const string scopeJson = """{"id":"scope-1","Type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}""";
        var workflowJson = $$"""{"id":"root","type":"Elsa.Sequence","version":1,"activities":[{{scopeJson}}]}""";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddElsa(elsa => elsa.UseWorkflowManagement(management =>
        {
            management.AddActivity<Sequence>();
            management.AddActivity<CompensationScope>();
            management.UseFailOnValidationErrors(false);
        }));
        await using var serviceProvider = services.BuildServiceProvider();
        await using var serviceScope = serviceProvider.CreateAsyncScope();
        var scopedServices = serviceScope.ServiceProvider;
        var activityRegistry = scopedServices.GetRequiredService<IActivityRegistry>();
        await activityRegistry.RegisterAsync<Sequence>();
        await activityRegistry.RegisterAsync<CompensationScope>();
        var workflowDefinitionService = scopedServices.GetRequiredService<IWorkflowDefinitionService>();
        var definition = new WorkflowDefinition
        {
            Id = "version-1",
            DefinitionId = "definition-1",
            Version = 1,
            MaterializerName = JsonWorkflowMaterializer.MaterializerName,
            StringData = workflowJson
        };

        var graph = await workflowDefinitionService.MaterializeWorkflowAsync(definition);
        var opaqueScope = Assert.IsType<NotFoundActivity>(Assert.Single(graph.Nodes.Select(node => node.Activity), activity => activity.Id == "scope-1"));
        Assert.Equal(scopeJson, opaqueScope.OriginalActivityJson);

        var result = await scopedServices.GetRequiredService<IWorkflowDefinitionPublisher>().PublishAsync(definition);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.ValidationErrors);
        Assert.Equal(CompensationScope.DraftOnlyDiagnosticCode, diagnostic.Code);
        Assert.Equal("scope-1", diagnostic.ActivityId);
    }

    public sealed class OpaquePayloadActivity : Activity
    {
        [JsonExtensionData]
        public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }
    }
}
