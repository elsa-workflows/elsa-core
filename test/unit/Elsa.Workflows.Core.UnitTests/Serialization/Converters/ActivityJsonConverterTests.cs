using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Expressions.Services;
using Elsa.Workflows.Activities;
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
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry
            .Find(WriteLineActivityTypeName)
            .Returns(new ActivityDescriptor { Constructor = (ctx) => WriteLineActivity });

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
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry
            .Find(WriteLineActivityTypeName, 1)
            .Returns(new ActivityDescriptor { Constructor = (ctx) => WriteLineActivity });

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
        var activityRegistry = CreateActivityRegistry(
            WorkflowAsActivityTypeName,
            new ActivityDescriptor
            {
                Constructor = (ctx) => WorkflowAsActivity
            }
        );
        var sut = CreateSut(activityRegistry);

        // Act
        var result = Execute(sut, WorkflowAsActivityJson_WithTypeNameOnly);

        // Assert
        Assert.Same(WorkflowAsActivity, result);
    }

    static IActivity? Execute(ActivityJsonConverter sut, string json)
    {
        return JsonSerializer.Deserialize<IActivity>(
            json,
            GetSerializerOptions(sut)
        );
    }

    static IActivityRegistry CreateActivityRegistry(string typeName, ActivityDescriptor? returnDescriptor)
    {
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry
            .Find(typeName)
            .Returns(returnDescriptor);

        return activityRegistry;
    }

    static IActivityRegistry CreateActivityRegistry_FindByCustomProperty(string customPropertyName, string customPropertyValue)
    {
        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry
            .Find(Arg.Any<Func<ActivityDescriptor, bool>>())
            .Returns(callInfo =>
            {
                var activityDescriptor = new ActivityDescriptor
                {
                    Constructor = (ctx) => WorkflowAsActivity,
                    CustomProperties =
                    {
                        [customPropertyName] = customPropertyValue
                    }
                };
                var predicate = callInfo.Arg<Func<ActivityDescriptor, bool>>();
                if (predicate(activityDescriptor))
                {
                    return activityDescriptor;
                }

                return null;
            });
        return activityRegistry;
    }

    static JsonSerializerOptions GetSerializerOptions(ActivityJsonConverter sut)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(sut);
        options.Converters.Add(new TypeJsonConverter(WellKnownTypeRegistry.CreateDefault()));
        options.Converters.Add(new PolymorphicObjectConverterFactory());

        var modifiers = new CustomConstructorConfigurator().GetModifiers();
        options.TypeInfoResolver = new ModifiableJsonTypeInfoResolver(modifiers);

        return options;
    }

    private const string UnknownActivityTypeName = "Some.Unknown.Activity";
    private const string WorkflowAsActivityTypeName = "Test-SubWorkflow";
    private const string WorkflowDefinitionIdCustomPropertyName = "WorkflowDefinitionId";
    private const string WorkflowDefinitionVersionIdCustomPropertyName = "WorkflowDefinitionVersionId";
    private const string WorkflowAsActivityDefinitionId = "bd94124913202141";
    private const string WorkflowAsActivityDefinitionVersionId = "aaa1112fff2443";
    private static readonly WriteLine WriteLineActivity = new("Hello world!");
    private static readonly string WriteLineActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<WriteLine>();
    private static readonly string NotFoundActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>();

    private const string WriteLineActivityJson_WithVersion = @"
        {
            ""text"": {
                ""typeName"": ""String"",
                ""expression"": {
                    ""type"": ""Literal"",
                    ""value"": ""Hello world!""
                }
            },
            ""id"": ""b54b024aa7469287"",
            ""nodeId"": ""Workflow1:51139b0cc83d3b07:b54b024aa7469287"",
            ""name"": ""WriteLine1"",
            ""type"": ""Elsa.WriteLine"",
            ""version"": 1,
            ""customProperties"": {
                ""canStartWorkflow"": false,
                ""runAsynchronously"": false
            },
            ""metadata"": {}
        }
    ";
    private const string UnknownActivityJson = @"
        {
            ""text"": {
                ""typeName"": ""String"",
                ""expression"": {
                    ""type"": ""Literal"",
                    ""value"": ""Hello world!""
                }
            },
            ""id"": ""b54b024aa7469287"",
            ""nodeId"": ""Workflow1:51139b0cc83d3b07:b54b024aa7469287"",
            ""name"": ""Unknown1"",
            ""type"": ""Some.Unknown.Activity"",
            ""customProperties"": {
                ""canStartWorkflow"": false,
                ""runAsynchronously"": false
            },
            ""metadata"": {}
        }
    ";
    private const string WriteLineActivityJson_WithoutVersion = @"
        {
            ""text"": {
                ""typeName"": ""String"",
                ""expression"": {
                    ""type"": ""Literal"",
                    ""value"": ""Hello world!""
                }
            },
            ""id"": ""b54b024aa7469287"",
            ""nodeId"": ""Workflow1:51139b0cc83d3b07:b54b024aa7469287"",
            ""name"": ""WriteLine1"",
            ""type"": ""Elsa.WriteLine"",
            ""customProperties"": {
                ""canStartWorkflow"": false,
                ""runAsynchronously"": false
            },
            ""metadata"": {}
        }
    ";

    private const string WorkflowAsActivityJson_WithVersionId = @"
    {
        ""workflowDefinitionVersionId"": ""aaa1112fff2443"",
        ""id"": ""5fc551ed366fe9fd"",
        ""nodeId"": ""Workflow2:cad9d2186550a8d7:5fc551ed366fe9fd"",
        ""name"": ""Test-SubWorkflow1"",
        ""type"": ""Test-SubWorkflow"",
        ""customProperties"": {
            ""canStartWorkflow"": false,
            ""runAsynchronously"": false,
            ""logPersistenceMode"": {
                ""default"": ""Inherit"",
                ""inputs"": {},
                ""outputs"": {}
            },
            ""logPersistenceConfig"": {
                ""default"": null,
                ""internalState"": null,
                ""inputs"": {},
                ""outputs"": {}
            }
        },
        ""metadata"": {}
    }";
    private const string WorkflowAsActivityJson_WithDefinitionId = @"
    {
        ""workflowDefinitionId"": ""bd94124913202141"",
        ""id"": ""5fc551ed366fe9fd"",
        ""nodeId"": ""Workflow2:cad9d2186550a8d7:5fc551ed366fe9fd"",
        ""name"": ""Test-SubWorkflow1"",
        ""type"": ""Test-SubWorkflow"",
        ""customProperties"": {
            ""canStartWorkflow"": false,
            ""runAsynchronously"": false,
            ""logPersistenceMode"": {
                ""default"": ""Inherit"",
                ""inputs"": {},
                ""outputs"": {}
            },
            ""logPersistenceConfig"": {
                ""default"": null,
                ""internalState"": null,
                ""inputs"": {},
                ""outputs"": {}
            }
        },
        ""metadata"": {}
    }";

    private const string WorkflowAsActivityJson_WithTypeNameOnly = @"
    {
        ""id"": ""5fc551ed366fe9fd"",
        ""nodeId"": ""Workflow2:cad9d2186550a8d7:5fc551ed366fe9fd"",
        ""name"": ""Test-SubWorkflow1"",
        ""type"": ""Test-SubWorkflow"",
        ""customProperties"": {
            ""canStartWorkflow"": false,
            ""runAsynchronously"": false,
            ""logPersistenceMode"": {
                ""default"": ""Inherit"",
                ""inputs"": {},
                ""outputs"": {}
            },
            ""logPersistenceConfig"": {
                ""default"": null,
                ""internalState"": null,
                ""inputs"": {},
                ""outputs"": {}
            }
        },
        ""metadata"": {}
    }";

    private static readonly IActivity WorkflowAsActivity = new WorkflowDefinitionActivity
    {
        WorkflowDefinitionId = WorkflowAsActivityDefinitionId,
        WorkflowDefinitionVersionId = WorkflowAsActivityDefinitionVersionId
    };

    private static ActivityJsonConverter CreateSut(IActivityRegistry activityRegistry)
    {
        var expressionDescriptorRegistry = new ExpressionDescriptorRegistry([]);
        return new ActivityJsonConverter(
            activityRegistry,
            expressionDescriptorRegistry,
            new ActivityWriter(
                activityRegistry,
                new SyntheticPropertiesWriter(expressionDescriptorRegistry),
                CreateLogger<ActivityWriter>()
            ),
            Substitute.For<IServiceProvider>()
        );
    }

    private static ILogger<T> CreateLogger<T>()
    {
        return LoggerFactory.Create(x => { }).CreateLogger<T>();
    }
}
