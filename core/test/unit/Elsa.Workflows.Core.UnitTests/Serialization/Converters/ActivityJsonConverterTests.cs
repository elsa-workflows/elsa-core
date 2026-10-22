using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Expressions.Services;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.Activities.StateMachine.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Configurators;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public sealed class ActivityJsonConverterTests
{
    [Fact]
    public void When_DeserializeKnownActivity_And_TypeNameSpecified_Then_FindsAndInstantiatesActivity()
    {
        // Arrange
        var activityRegistry = CreateActivityRegistry(WriteLineActivityTypeName, WriteLineActivity);
        var sut = CreateSut(activityRegistry);

        // Act
        var result = Execute(sut, WriteLineActivityJson_WithoutVersion);

        // Assert
        Assert.Same(WriteLineActivity, result);
    }

    [Fact]
    public void When_DeserializeKnownActivity_And_TypeNameSpecified_And_VersionSpecified_Then_FindsAndInstantiatesActivity()
    {
        // Arrange
        var activityRegistry = CreateActivityRegistry(WriteLineActivityTypeName, WriteLineActivity, version: 1);
        var sut = CreateSut(activityRegistry);

        // Act
        var result = Execute(sut, WriteLineActivityJson_WithVersion);

        // Assert
        Assert.Same(WriteLineActivity, result);
    }

    [Fact]
    public void When_DeserializeUnknownActivity_Then_ReturnsNotFoundActivity()
    {
        // Arrange
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry
            .Find(NotFoundActivityTypeName)
            .Returns(new ActivityDescriptor());

        var sut = CreateSut(activityRegistry);

        // Act
        var result = Execute(sut, UnknownActivityJson);

        // Assert
        Assert.IsType<NotFoundActivity>(result);
        var notFoundActivity = (NotFoundActivity)result;
        Assert.Equal(UnknownActivityTypeName, notFoundActivity.MissingTypeName);
        Assert.Equal(0, notFoundActivity.MissingTypeVersion);

        var expectedJsonDoc = JsonDocument.Parse(UnknownActivityJson);
        var actualJsonDoc = JsonDocument.Parse(notFoundActivity.OriginalActivityJson);
        Assert.Equal(expectedJsonDoc.RootElement.ToString(), actualJsonDoc.RootElement.ToString());
        Assert.True(notFoundActivity.Metadata.ContainsKey("displayText"));
        Assert.True(notFoundActivity.Metadata.ContainsKey("description"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void When_DeserializeCompensationScopeWithUnsupportedPin_Then_PreservesItOpaque(bool wrapped)
    {
        const string compensationScopeJson = """
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[]}
            """;
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry.Find(NotFoundActivityTypeName).Returns(new ActivityDescriptor { Constructor = _ => new(new NotFoundActivity()) });
        activityRegistry.Find(CompensationScope.ActivityTypeName).Returns(new ActivityDescriptor
        {
            Version = 2,
            Constructor = _ => new(new CompensationScope())
        });
        activityRegistry.Find(CompensationScope.ActivityTypeName, 1).Returns((ActivityDescriptor?)null);

        var json = wrapped
            ? JsonSerializer.Serialize(new { type = NotFoundActivityTypeName, version = 1, originalActivityJson = compensationScopeJson })
            : compensationScopeJson;
        var result = Assert.IsType<NotFoundActivity>(Execute(CreateSut(activityRegistry), json));

        using var expected = JsonDocument.Parse(compensationScopeJson);
        using var actual = JsonDocument.Parse(result.OriginalActivityJson);
        Assert.Equal(expected.RootElement.GetRawText(), actual.RootElement.GetRawText());
        Assert.Equal(CompensationScope.ActivityTypeName, result.MissingTypeName);
        Assert.Equal(1, result.MissingTypeVersion);
    }

    [Fact]
    public void When_DeserializeCompensationScopeWithUnavailableChildPin_Then_PreservesTheWholeScopeOpaque()
    {
        var compensationScopeJson = $$"""
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","forward":{"id":"forward-1","type":"{{WriteLineActivityTypeName}}","version":1},"handler":null,"mappings":[]}]}
            """;
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry.Find(NotFoundActivityTypeName).Returns(new ActivityDescriptor { Constructor = _ => new(new NotFoundActivity()) });
        activityRegistry.Find(CompensationScope.ActivityTypeName).Returns(new ActivityDescriptor { Version = 1, Constructor = _ => new(new CompensationScope()) });
        activityRegistry.Find(CompensationScope.ActivityTypeName, 1).Returns(new ActivityDescriptor { Version = 1, Constructor = _ => new(new CompensationScope()) });
        activityRegistry.Find(WriteLineActivityTypeName).Returns(new ActivityDescriptor { Version = 2 });
        activityRegistry.Find(WriteLineActivityTypeName, 1).Returns((ActivityDescriptor?)null);

        var result = Assert.IsType<NotFoundActivity>(Execute(CreateSut(activityRegistry), compensationScopeJson));

        using var expected = JsonDocument.Parse(compensationScopeJson);
        using var actual = JsonDocument.Parse(result.OriginalActivityJson);
        Assert.Equal(expected.RootElement.GetRawText(), actual.RootElement.GetRawText());
        Assert.Equal(CompensationScope.ActivityTypeName, result.MissingTypeName);
        Assert.Equal(1, result.MissingTypeVersion);
    }

    [Fact]
    public void When_DeserializeCompensationScopeWithNullMappings_Then_PreservesTheWholeScopeOpaque()
    {
        const string compensationScopeJson = """
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","forward":null,"handler":null,"mappings":null}]}
            """;
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry.Find(NotFoundActivityTypeName).Returns(new ActivityDescriptor { Constructor = _ => new(new NotFoundActivity()) });
        activityRegistry.Find(CompensationScope.ActivityTypeName).Returns(new ActivityDescriptor { Version = 1 });
        activityRegistry.Find(CompensationScope.ActivityTypeName, 1).Returns(new ActivityDescriptor { Version = 1 });

        var result = Assert.IsType<NotFoundActivity>(Execute(CreateSut(activityRegistry), compensationScopeJson));

        using var expected = JsonDocument.Parse(compensationScopeJson);
        using var actual = JsonDocument.Parse(result.OriginalActivityJson);
        Assert.Equal(expected.RootElement.GetRawText(), actual.RootElement.GetRawText());
        Assert.Equal(JsonValueKind.Null, actual.RootElement.GetProperty("steps")[0].GetProperty("mappings").ValueKind);
    }

    [Fact]
    public void When_DeserializeCompensationScopeWithLiteralInputType_Then_MaterializesScope()
    {
        var template = """
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","forward":{"id":"forward-1","type":"WRITE_LINE_TYPE","version":1,"text":{"typeName":"String","expression":{"type":"Literal","value":"hello"}}},"handler":null,"mappings":[]}]}
            """;
        var compensationScopeJson = template.Replace("WRITE_LINE_TYPE", WriteLineActivityTypeName, StringComparison.Ordinal);
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry.Find(NotFoundActivityTypeName).Returns(new ActivityDescriptor { Constructor = _ => new(new NotFoundActivity()) });
        activityRegistry.Find(CompensationScope.ActivityTypeName).Returns(new ActivityDescriptor { Version = 1, Constructor = _ => new(new CompensationScope()) });
        activityRegistry.Find(CompensationScope.ActivityTypeName, 1).Returns(new ActivityDescriptor { Version = 1, Constructor = _ => new(new CompensationScope()) });
        activityRegistry.Find(WriteLineActivityTypeName, 1).Returns(new ActivityDescriptor { Version = 1, ClrType = typeof(WriteLine) });

        var result = Assert.IsType<CompensationScope>(Execute(CreateSut(activityRegistry), compensationScopeJson));
    }

    [Fact]
    public void When_DeserializeCompensationScopeWithUnavailableNestedChildPin_Then_PreservesTheWholeScopeOpaque()
    {
        var template = """
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","forward":{"id":"sequence-1","type":"SEQUENCE_TYPE","version":1,"activities":[{"id":"nested-1","type":"WRITE_LINE_TYPE","version":2}]},"handler":null,"mappings":[]}]}
            """;
        var compensationScopeJson = template
            .Replace("SEQUENCE_TYPE", ActivityTypeNameHelper.GenerateTypeName<Sequence>(), StringComparison.Ordinal)
            .Replace("WRITE_LINE_TYPE", WriteLineActivityTypeName, StringComparison.Ordinal);
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry.Find(NotFoundActivityTypeName).Returns(new ActivityDescriptor { Constructor = _ => new(new NotFoundActivity()) });
        activityRegistry.Find(CompensationScope.ActivityTypeName).Returns(new ActivityDescriptor { Version = 1 });
        activityRegistry.Find(CompensationScope.ActivityTypeName, 1).Returns(new ActivityDescriptor { Version = 1 });
        activityRegistry.Find(ActivityTypeNameHelper.GenerateTypeName<Sequence>(), 1).Returns(new ActivityDescriptor { Version = 1, ClrType = typeof(Sequence) });
        activityRegistry.Find(WriteLineActivityTypeName, 2).Returns((ActivityDescriptor?)null);

        var result = Assert.IsType<NotFoundActivity>(Execute(CreateSut(activityRegistry), compensationScopeJson));

        using var expected = JsonDocument.Parse(compensationScopeJson);
        using var actual = JsonDocument.Parse(result.OriginalActivityJson);
        Assert.Equal(expected.RootElement.GetRawText(), actual.RootElement.GetRawText());
    }

    [Theory]
    [InlineData("entry")]
    [InlineData("exit")]
    [InlineData("trigger")]
    [InlineData("action")]
    public void When_DeserializeCompensationScopeWithUnavailableStateMachineOwnedPin_Then_PreservesTheWholeScopeOpaque(string role)
    {
        var compensationScopeJson = CreateStateMachineCompensationScopeJson(role, 2);
        var activityRegistry = CreateStateMachineCompensationRegistry();

        var result = Assert.IsType<NotFoundActivity>(Execute(CreateSut(activityRegistry), compensationScopeJson));

        Assert.Equal(CompensationScope.ActivityTypeName, result.MissingTypeName);
        Assert.Equal(compensationScopeJson, result.OriginalActivityJson);
    }

    private static string CreateStateMachineCompensationScopeJson(string unpinnedRole, int version)
    {
        var child = (string role) => unpinnedRole == "all" || role == unpinnedRole
            ? JsonSerializer.Serialize(new { id = $"{role}-1", type = WriteLineActivityTypeName, version })
            : "null";
        var stateMachineTypeName = ActivityTypeNameHelper.GenerateTypeName<StateMachine>();
        return $$"""
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"step-1","forward":{"id":"machine-1","type":"{{stateMachineTypeName}}","version":1,"states":[{"name":"state","entry":{{child("entry")}},"exit":{{child("exit")}}}],"transitions":[{"from":"state","to":"state","trigger":{{child("trigger")}},"action":{{child("action")}}}]},"handler":null,"mappings":[]}]}
            """;
    }

    private static IActivityRegistry CreateStateMachineCompensationRegistry()
    {
        var activityRegistry = Substitute.For<IActivityRegistry>();
        var scopeDescriptor = new ActivityDescriptor { Version = 1, ClrType = typeof(CompensationScope), Constructor = context => context.CreateActivity<CompensationScope>() };
        var stateMachineDescriptor = new ActivityDescriptor { Version = 1, ClrType = typeof(StateMachine), Constructor = context => context.CreateActivity<StateMachine>() };
        var writeLineDescriptor = new ActivityDescriptor { Version = 1, ClrType = typeof(WriteLine), Constructor = context => context.CreateActivity<WriteLine>() };
        var notFoundDescriptor = new ActivityDescriptor { Constructor = _ => new(new NotFoundActivity()) };
        var stateMachineTypeName = ActivityTypeNameHelper.GenerateTypeName<StateMachine>();

        activityRegistry.Find(CompensationScope.ActivityTypeName).Returns(scopeDescriptor);
        activityRegistry.Find(CompensationScope.ActivityTypeName, 1).Returns(scopeDescriptor);
        activityRegistry.Find(stateMachineTypeName, 1).Returns(stateMachineDescriptor);
        activityRegistry.Find(WriteLineActivityTypeName, 1).Returns(writeLineDescriptor);
        activityRegistry.Find(NotFoundActivityTypeName).Returns(notFoundDescriptor);
        return activityRegistry;
    }

    [Fact]
    public void When_DeserializeCompensationScopeWithDuplicateStepIds_Then_PreservesTheWholeScopeOpaque()
    {
        const string compensationScopeJson = """
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"steps":[{"stepId":"same","forward":null,"handler":null,"mappings":[]},{"stepId":"same","forward":null,"handler":null,"mappings":[]}]}
            """;
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry.Find(NotFoundActivityTypeName).Returns(new ActivityDescriptor { Constructor = _ => new(new NotFoundActivity()) });
        activityRegistry.Find(CompensationScope.ActivityTypeName).Returns(new ActivityDescriptor { Version = 1 });
        activityRegistry.Find(CompensationScope.ActivityTypeName, 1).Returns(new ActivityDescriptor { Version = 1 });

        var result = Assert.IsType<NotFoundActivity>(Execute(CreateSut(activityRegistry), compensationScopeJson));

        Assert.Equal(compensationScopeJson, result.OriginalActivityJson);
    }

    [Theory]
    [InlineData("\"future\"")]
    [InlineData("null")]
    public void When_DeserializeCompensationScopeWithNonNumericVersion_Then_PreservesItOpaque(string versionJson)
    {
        var compensationScopeJson = $$"""
            {"id":"scope-1","type":"Elsa.CompensationScope","version":{{versionJson}},"draftSchemaVersion":1,"steps":[]}
            """;
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry.Find(NotFoundActivityTypeName).Returns(new ActivityDescriptor { Constructor = _ => new(new NotFoundActivity()) });
        activityRegistry.Find(CompensationScope.ActivityTypeName).Returns(new ActivityDescriptor { Version = 1 });

        var result = Assert.IsType<NotFoundActivity>(Execute(CreateSut(activityRegistry), compensationScopeJson));

        Assert.Equal(0, result.MissingTypeVersion);
        Assert.Equal(compensationScopeJson, result.OriginalActivityJson);
    }

    [Fact]
    public void When_DeserializeWorkflowAsActivity_And_WorkflowDefinitionIdSpecified_Then_FindsAndInstantiatesActivity()
    {
        // Arrange
        var activityRegistry = CreateActivityRegistry_FindByCustomProperty(
            WorkflowDefinitionIdCustomPropertyName,
            WorkflowAsActivityDefinitionId
        );

        var sut = CreateSut(activityRegistry);

        // Act
        var result = Execute(sut, WorkflowAsActivityJson_WithDefinitionId);

        // Assert
        Assert.Same(WorkflowAsActivity, result);
    }

    [Fact]
    public void When_DeserializeWorkflowAsActivity_And_WorkflowDefinitionVersionIdSpecified_Then_FindsAndInstantiatesActivity()
    {
        // Arrange
        var activityRegistry = CreateActivityRegistry_FindByCustomProperty(
            WorkflowDefinitionVersionIdCustomPropertyName,
            WorkflowAsActivityDefinitionVersionId
        );
        var sut = CreateSut(activityRegistry);

        // Act
        var result = Execute(sut, WorkflowAsActivityJson_WithVersionId);

        // Assert
        Assert.Same(WorkflowAsActivity, result);
    }

    [Fact]
    public void When_DeserializeWorkflowAsActivity_And_TypeNameSpecified_Then_FindsAndInstantiatesActivity()
    {
        // Arrange
        var activityRegistry = CreateActivityRegistry(WorkflowAsActivityTypeName, WorkflowAsActivity);
        var sut = CreateSut(activityRegistry);

        // Act
        var result = Execute(sut, WorkflowAsActivityJson_WithTypeNameOnly);

        // Assert
        Assert.Same(WorkflowAsActivity, result);
    }

    static IActivity? Execute(ActivityJsonConverter sut, string json) =>
        JsonSerializer.Deserialize<IActivity>(json, GetSerializerOptions(sut));

    static IActivityRegistry CreateActivityRegistry(string typeName, IActivity activity, int? version = null)
    {
        var descriptor = new ActivityDescriptor { Constructor = _ => new(activity) };
        var activityRegistry = Substitute.For<IActivityRegistry>();

        if (version.HasValue)
            activityRegistry.Find(typeName, version.Value).Returns(descriptor);
        else
            activityRegistry.Find(typeName).Returns(descriptor);

        return activityRegistry;
    }

    static IActivityRegistry CreateActivityRegistry_FindByCustomProperty(string customPropertyName, string customPropertyValue)
    {
        var descriptor = new ActivityDescriptor
        {
            Constructor = _ => new(WorkflowAsActivity),
            CustomProperties = { [customPropertyName] = customPropertyValue }
        };

        var activityRegistry = Substitute.For<IActivityRegistry>();

        // Mock type name lookups to return null, ensuring the custom property lookup path is tested
        activityRegistry.Find(Arg.Any<string>()).Returns((ActivityDescriptor?)null);
        activityRegistry.Find(Arg.Any<string>(), Arg.Any<int>()).Returns((ActivityDescriptor?)null);

        activityRegistry
            .Find(Arg.Any<Func<ActivityDescriptor, bool>>())
            .Returns(callInfo => callInfo.Arg<Func<ActivityDescriptor, bool>>()(descriptor) ? descriptor : null);

        return activityRegistry;
    }

    static JsonSerializerOptions GetSerializerOptions(ActivityJsonConverter sut) => new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            sut,
            new TypeJsonConverter(),
            new PolymorphicObjectConverterFactory()
        },
        TypeInfoResolver = new ModifiableJsonTypeInfoResolver(new CustomConstructorConfigurator().GetModifiers())
    };

    private const string UnknownActivityTypeName = "Some.Unknown.Activity";
    private const string WorkflowAsActivityTypeName = "Test-SubWorkflow";
    private const string WorkflowDefinitionIdCustomPropertyName = "WorkflowDefinitionId";
    private const string WorkflowDefinitionVersionIdCustomPropertyName = "WorkflowDefinitionVersionId";
    private const string WorkflowAsActivityDefinitionId = "bd94124913202141";
    private const string WorkflowAsActivityDefinitionVersionId = "aaa1112fff2443";
    private static readonly WriteLine WriteLineActivity = new("Hello world!");
    private static readonly string WriteLineActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<WriteLine>();
    private static readonly string NotFoundActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>();

    private const string WriteLineActivityJson_WithVersion = 
"""
 {
     "text": {
         "typeName": "String",
         "expression": {
             "type": "Literal",
             "value": "Hello world!"
         }
     },
     "id": "b54b024aa7469287",
     "nodeId": "Workflow1:51139b0cc83d3b07:b54b024aa7469287",
     "name": "WriteLine1",
     "type": "Elsa.WriteLine",
     "version": 1,
     "customProperties": {
         "canStartWorkflow": false,
         "runAsynchronously": false
     },
     "metadata": {}
 }
 """;

    private const string UnknownActivityJson = 
"""
{
   "text": {
       "typeName": "String",
       "expression": {
           "type": "Literal",
           "value": "Hello world!"
       }
   },
   "id": "b54b024aa7469287",
   "nodeId": "Workflow1:51139b0cc83d3b07:b54b024aa7469287",
   "name": "Unknown1",
   "type": "Some.Unknown.Activity",
   "customProperties": {
       "canStartWorkflow": false,
       "runAsynchronously": false
   },
   "metadata": {}
}
""";

    private const string WriteLineActivityJson_WithoutVersion = 
"""
{
    "text": {
        "typeName": "String",
        "expression": {
            "type": "Literal",
            "value": "Hello world!"
        }
    },
    "id": "b54b024aa7469287",
    "nodeId": "Workflow1:51139b0cc83d3b07:b54b024aa7469287",
    "name": "WriteLine1",
    "type": "Elsa.WriteLine",
    "customProperties": {
        "canStartWorkflow": false,
        "runAsynchronously": false
    },
    "metadata": {}
}
""";

    private const string WorkflowAsActivityJson_WithVersionId = 
"""
{
    "workflowDefinitionVersionId": "aaa1112fff2443",
    "id": "5fc551ed366fe9fd",
    "nodeId": "Workflow2:cad9d2186550a8d7:5fc551ed366fe9fd",
    "name": "Test-SubWorkflow1",
    "type": "Test-SubWorkflow",
    "customProperties": {
        "canStartWorkflow": false,
        "runAsynchronously": false,
        "logPersistenceMode": {
            "default": "Inherit",
            "inputs": {},
            "outputs": {}
        },
        "logPersistenceConfig": {
            "default": null,
            "internalState": null,
            "inputs": {},
            "outputs": {}
        }
    },
    "metadata": {}
}
""";

    private const string WorkflowAsActivityJson_WithDefinitionId = 
"""
{
   "workflowDefinitionId": "bd94124913202141",
   "id": "5fc551ed366fe9fd",
   "nodeId": "Workflow2:cad9d2186550a8d7:5fc551ed366fe9fd",
   "name": "Test-SubWorkflow1",
   "type": "Test-SubWorkflow",
   "customProperties": {
       "canStartWorkflow": false,
       "runAsynchronously": false,
       "logPersistenceMode": {
           "default": "Inherit",
           "inputs": {},
           "outputs": {}
       },
       "logPersistenceConfig": {
           "default": null,
           "internalState": null,
           "inputs": {},
           "outputs": {}
       }
   },
   "metadata": {}
}
""";

    private const string WorkflowAsActivityJson_WithTypeNameOnly = 
"""
{
   "id": "5fc551ed366fe9fd",
   "nodeId": "Workflow2:cad9d2186550a8d7:5fc551ed366fe9fd",
   "name": "Test-SubWorkflow1",
   "type": "Test-SubWorkflow",
   "customProperties": {
       "canStartWorkflow": false,
       "runAsynchronously": false,
       "logPersistenceMode": {
           "default": "Inherit",
           "inputs": {},
           "outputs": {}
       },
       "logPersistenceConfig": {
           "default": null,
           "internalState": null,
           "inputs": {},
           "outputs": {}
       }
   },
   "metadata": {}
}
""";

    private static readonly IActivity WorkflowAsActivity = new WorkflowDefinitionActivity
    {
        WorkflowDefinitionId = WorkflowAsActivityDefinitionId,
        WorkflowDefinitionVersionId = WorkflowAsActivityDefinitionVersionId
    };

    private static ActivityJsonConverter CreateSut(IActivityRegistry activityRegistry)
    {
        var expressionDescriptorRegistry = new ExpressionDescriptorRegistry([]);
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider
            .GetService(typeof(ILogger<ActivityJsonConverter>))!
            .Returns(CreateLogger<ActivityJsonConverter>());

        var activityWriter = new ActivityWriter(
            activityRegistry,
            new(expressionDescriptorRegistry),
            CreateLogger<ActivityWriter>());
        serviceProvider.GetService(typeof(ActivityWriter)).Returns(activityWriter);

        return new(
            activityRegistry,
            expressionDescriptorRegistry,
            activityWriter,
            serviceProvider
        );
    }

    private static ILogger<T> CreateLogger<T>() => LoggerFactory.Create(_ => { }).CreateLogger<T>();
}
