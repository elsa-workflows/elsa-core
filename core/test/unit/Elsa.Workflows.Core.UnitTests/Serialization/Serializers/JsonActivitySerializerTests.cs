using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.Activities.StateMachine.Activities;
using Elsa.Workflows.Activities.StateMachine.Models;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Serializers;

public sealed class JsonActivitySerializerTests : IAsyncLifetime
{
    private readonly ServiceProvider _services;

    public JsonActivitySerializerTests()
    {
        var services = new ServiceCollection();
        services.AddElsa();
        _services = services.BuildServiceProvider();
    }
    private IActivitySerializer Serializer => _services.GetRequiredService<IActivitySerializer>();

    public async Task InitializeAsync()
    {
        var registry = _services.GetRequiredService<IActivityRegistry>();
        await registry.RegisterAsync<Sequence>();
        await registry.RegisterAsync<If>();
        await registry.RegisterAsync<WriteLine>();
        await registry.RegisterAsync<CompensationScope>();
        await registry.RegisterAsync<StateMachine>();
        registry.Register(new ActivityDescriptor
        {
            TypeName = "Test.Generated",
            ClrType = typeof(GeneratedActivity),
            Version = 2,
            Constructor = context => context.CreateActivity<GeneratedActivity>(),
            Inputs = { new InputDescriptor { Name = "Text", Type = typeof(string), IsSynthetic = true } },
            Outputs = { new OutputDescriptor { Name = "Result", Type = typeof(string), IsSynthetic = true } }
        });
    }

    public Task DisposeAsync() => _services.DisposeAsync().AsTask();

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void RoundTripPreservesGeneratedInputsAndOutputs(bool objectOverload, bool nested)
    {
        var generated = new GeneratedActivity { Id = "generated", Type = "Test.Generated", Version = 2 };
        generated.SyntheticProperties["Text"] = new Input<string>("keep this value");
        generated.SyntheticProperties["Result"] = new Output<string>(new Variable("result"));
        IActivity root = nested ? new Sequence { Id = "root", Activities = [generated] } : generated;

        var json = objectOverload ? Serializer.Serialize((object)root) : Serializer.Serialize(root);
        using var document = JsonDocument.Parse(json);
        var node = nested ? document.RootElement.GetProperty("activities")[0] : document.RootElement;
        Assert.Equal("keep this value", node.GetProperty("text").GetProperty("expression").GetProperty("value").GetString());
        var restoredRoot = Serializer.Deserialize(json);
        var restored = Assert.IsType<GeneratedActivity>(nested ? Assert.IsType<Sequence>(restoredRoot).Activities.Single() : restoredRoot);
        Assert.Equal(generated.Id, restored.Id);
        Assert.Equal(generated.Type, restored.Type);
        Assert.Equal(generated.Version, restored.Version);
        Assert.IsType<Input<string>>(restored.SyntheticProperties["Text"]);
        var output = Assert.IsType<Output<string>>(restored.SyntheticProperties["Result"]);
        Assert.Equal("resultVariable", output.MemoryBlockReference().Id);
        using var roundTrip = JsonDocument.Parse(Serializer.Serialize(restored));
        Assert.Equal("keep this value", roundTrip.RootElement.GetProperty("text").GetProperty("expression").GetProperty("value").GetString());
    }

    [Fact]
    public void StaticActivityRoundTripPreservesInputAndIdentity()
    {
        var activity = new WriteLine("hello") { Id = "write" };
        var restored = Assert.IsType<WriteLine>(Serializer.Deserialize(Serializer.Serialize(activity)));
        Assert.Equal(activity.Id, restored.Id);
        Assert.Equal(activity.Type, restored.Type);
        using var document = JsonDocument.Parse(Serializer.Serialize(restored));
        Assert.Equal("hello", document.RootElement.GetProperty("text").GetProperty("expression").GetProperty("value").GetString());
    }

    [Fact]
    public void NestedOrdinaryActivitiesPreserveNullsAndExplicitJsonIgnoreConditions()
    {
        var conditional = new If { Id = "if-1" };
        var generated = new GeneratedActivity
        {
            Id = "generated-1",
            Type = "Test.Generated",
            Version = 2,
            Omitted = null,
            ConditionalNull = null,
            RetainedNull = null,
            Number = 0
        };
        var sequence = new Sequence { Id = "sequence-1", Activities = [conditional, generated] };

        using var document = JsonDocument.Parse(Serializer.Serialize(sequence));
        var activities = document.RootElement.GetProperty("activities");
        var serializedIf = activities[0];
        var serializedGenerated = activities[1];

        Assert.Equal(JsonValueKind.Null, serializedIf.GetProperty("then").ValueKind);
        Assert.Equal(JsonValueKind.Null, serializedIf.GetProperty("else").ValueKind);
        Assert.Equal(JsonValueKind.Null, serializedGenerated.GetProperty("omitted").ValueKind);
        Assert.False(serializedGenerated.TryGetProperty("conditionalNull", out _));
        Assert.Equal(JsonValueKind.Null, serializedGenerated.GetProperty("retainedNull").ValueKind);
        Assert.False(serializedGenerated.TryGetProperty("number", out _));
    }

    [Fact]
    public void CompensationScopeDraftRoundTripPreservesMappingsAndExtensionData()
    {
        const string json = """
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"scopeExtension":{"_type":"FutureScopeExtension","items":[{"_type":"FutureItem","values":[1,2]}]},"steps":[{"stepId":"step-1","name":"row","forward":null,"handler":null,"mappings":[{"_type":"FutureMapping","items":[{"mappingId":"mapping-1","source":"capture path","futureMapping":7}],"values":[{"_type":"NestedMapping","items":[1]}]}],"stepExtension":{"_type":"FutureStepExtension","values":["future"]}}]}
            """;

        var scope = Assert.IsType<CompensationScope>(Serializer.Deserialize(json));
        using var roundTrip = JsonDocument.Parse(Serializer.Serialize(scope));
        var root = roundTrip.RootElement;
        var step = root.GetProperty("steps")[0];

        Assert.Equal("FutureScopeExtension", root.GetProperty("scopeExtension").GetProperty("_type").GetString());
        Assert.Equal(2, root.GetProperty("scopeExtension").GetProperty("items")[0].GetProperty("values").GetArrayLength());
        Assert.Equal("step-1", step.GetProperty("stepId").GetString());
        Assert.Equal("FutureStepExtension", step.GetProperty("stepExtension").GetProperty("_type").GetString());
        Assert.Equal("future", step.GetProperty("stepExtension").GetProperty("values")[0].GetString());
        var mapping = step.GetProperty("mappings")[0];
        Assert.Equal("FutureMapping", mapping.GetProperty("_type").GetString());
        Assert.Equal("mapping-1", mapping.GetProperty("items")[0].GetProperty("mappingId").GetString());
        Assert.Equal("capture path", mapping.GetProperty("items")[0].GetProperty("source").GetString());
        Assert.Equal(7, mapping.GetProperty("items")[0].GetProperty("futureMapping").GetInt32());
        Assert.Equal(1, mapping.GetProperty("values")[0].GetProperty("items")[0].GetInt32());
    }

    [Fact]
    public void CompensationDraftPreservesWholeRawJsonWhenForwardAliasContainsUnavailablePin()
    {
        var step = $$"""{"stepId":"step-1","Forward":{{CreateWriteLineJson("forward-future", 99)}},"handler":null,"mappings":[]}""";
        var json = CreateCompensationDraftJson(step);

        AssertOpaqueCompensationDraftRoundTrips(json);
    }

    [Fact]
    public void CompensationDraftPreservesWholeRawJsonWhenForwardAndForwardAliasConflict()
    {
        var step = $$"""{"stepId":"step-1","forward":{{CreateWriteLineJson("forward-pinned", 1)}},"Forward":{{CreateWriteLineJson("forward-future", 99)}},"handler":null,"mappings":[]}""";
        var json = CreateCompensationDraftJson(step);

        AssertOpaqueCompensationDraftRoundTrips(json);
    }

    [Fact]
    public void CompensationDraftWithCaseAliasedTypeIsPreservedWholeAndOpaque()
    {
        var step = """{"stepId":"step-1","forward":null,"handler":null,"mappings":[]}""";
        var json = CreateCompensationDraftJson(step)
            .Replace("\"type\":\"Elsa.CompensationScope\"", "\"Type\":\"Elsa.CompensationScope\"", StringComparison.Ordinal);

        var restored = Assert.IsType<NotFoundActivity>(Serializer.Deserialize(json));

        Assert.Equal(CompensationScope.ActivityTypeName, restored.MissingTypeName);
        Assert.Equal(json, restored.OriginalActivityJson);
        AssertOpaqueSerializedCompensationDraft(Serializer, restored, json);
    }

    [Theory]
    [InlineData("draftSchemaVersion", "DraftSchemaVersion", "1", "2")]
    [InlineData("onForwardFault", "OnForwardFault", "\"Compensated\"", "\"PropagateOriginalFault\"")]
    [InlineData("stepId", "StepId", "\"step-1\"", "\"replacement-step\"")]
    [InlineData("mappings", "Mappings", "[]", "[{\"mappingId\":\"future\"}]")]
    public void CompensationDraftPreservesWholeRawJsonWhenKnownPropertyHasCaseAlias(string propertyName, string aliasName, string canonicalValue, string aliasValue)
    {
        const string step = "{\"stepId\":\"step-1\",\"forward\":null,\"handler\":null,\"mappings\":[]}";
        var json = CreateCompensationDraftJson(step);
        var ambiguousJson = AddCaseAlias(json, propertyName, aliasName, canonicalValue, aliasValue);

        AssertOpaqueCompensationDraftRoundTrips(ambiguousJson);
    }

    [Theory]
    [InlineData("type", "Type", "\"Elsa.WriteLine\"", "\"Elsa.FutureWriteLine\"")]
    [InlineData("version", "Version", "1", "99")]
    public void CompensationDraftPreservesWholeRawJsonWhenChildPinHasCaseAlias(string propertyName, string aliasName, string canonicalValue, string aliasValue)
    {
        var child = AddCaseAlias(CreateWriteLineJson("forward-1", 1), propertyName, aliasName, canonicalValue, aliasValue);
        var step = $$"""{"stepId":"step-1","forward":{{child}},"handler":null,"mappings":[]}""";
        var json = CreateCompensationDraftJson(step);

        AssertOpaqueCompensationDraftRoundTrips(json);
    }

    [Fact]
    public void OrdinaryActivityWithUnknownVersionStillUsesLatestFallback()
    {
        var json = "{\"id\":\"write-1\",\"type\":\"Elsa.WriteLine\",\"version\":99}";

        var restored = Assert.IsType<WriteLine>(Serializer.Deserialize(json));

        Assert.Equal(99, restored.Version);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void UnsupportedCompensationDraftPreservesRawJsonAndOuterDesignerMetadata(bool wrapped, bool unsupportedVersion)
    {
        var version = unsupportedVersion ? 2 : 1;
        var draftSchemaVersion = unsupportedVersion ? 1 : 2;
        var originalScopePosition = new { x = 10, y = 20 };
        var wrapperPosition = new { x = 30, y = 40 };
        var originalScopeJson = JsonSerializer.Serialize(new
        {
            id = "scope-1",
            type = CompensationScope.ActivityTypeName,
            version,
            draftSchemaVersion,
            steps = Array.Empty<object>(),
            metadata = new { designer = new { position = originalScopePosition, size = new { width = 220, height = 80 } } }
        });
        var json = wrapped
            ? JsonSerializer.Serialize(new
            {
                id = "placeholder-1",
                type = ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>(),
                version = 1,
                metadata = new { designer = new { position = wrapperPosition, size = new { width = 320, height = 120 } } },
                originalActivityJson = originalScopeJson
            })
            : originalScopeJson;
        var expectedPosition = wrapped ? wrapperPosition : originalScopePosition;

        var activity = Assert.IsType<NotFoundActivity>(Serializer.Deserialize(json));
        Assert.Equal("scope-1", activity.Id);
        Assert.Equal(CompensationScope.ActivityTypeName, activity.MissingTypeName);
        Assert.Equal(originalScopeJson, activity.OriginalActivityJson);
        AssertDesignerPosition(activity, expectedPosition.x, expectedPosition.y);

        var serialized = Serializer.Serialize(activity);
        using var serializedDocument = JsonDocument.Parse(serialized);
        Assert.Equal(originalScopeJson, serializedDocument.RootElement.GetProperty("originalActivityJson").GetString());
        var serializedPosition = serializedDocument.RootElement.GetProperty("metadata").GetProperty("designer").GetProperty("position");
        Assert.Equal(expectedPosition.x, serializedPosition.GetProperty("x").GetInt32());
        Assert.Equal(expectedPosition.y, serializedPosition.GetProperty("y").GetInt32());

        var restored = Assert.IsType<NotFoundActivity>(Serializer.Deserialize(serialized));
        Assert.Equal(originalScopeJson, restored.OriginalActivityJson);
        AssertDesignerPosition(restored, expectedPosition.x, expectedPosition.y);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MalformedOpaqueCompensationMetadataDoesNotPreventRoundTrip(bool wrapped)
    {
        var originalScopeJson = wrapped
            ? """
              {"id":"scope-1","type":"Elsa.CompensationScope","version":2,"draftSchemaVersion":1,"steps":[],"metadata":{"designer":{"position":{"x":11,"y":12}}}}
              """
            : """
              {"id":"scope-1","type":"Elsa.CompensationScope","version":2,"draftSchemaVersion":1,"steps":[],"metadata":[]}
              """;
        using var malformedMetadata = JsonDocument.Parse("[]");
        var json = wrapped
            ? JsonSerializer.Serialize(new
            {
                type = ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>(),
                version = 1,
                metadata = malformedMetadata.RootElement,
                originalActivityJson = originalScopeJson
            })
            : originalScopeJson;

        var activity = Assert.IsType<NotFoundActivity>(Serializer.Deserialize(json));
        Assert.Equal(originalScopeJson, activity.OriginalActivityJson);
        if (wrapped)
            AssertDesignerPosition(activity, 11, 12);

        var serialized = Serializer.Serialize(activity);
        var restored = Assert.IsType<NotFoundActivity>(Serializer.Deserialize(serialized));
        Assert.Equal(originalScopeJson, restored.OriginalActivityJson);
        if (wrapped)
            AssertDesignerPosition(restored, 11, 12);
    }

    private static void AssertDesignerPosition(NotFoundActivity activity, int expectedX, int expectedY)
    {
        var designer = Assert.IsType<JsonElement>(activity.Metadata["designer"]);
        var position = designer.GetProperty("position");
        Assert.Equal(expectedX, position.GetProperty("x").GetInt32());
        Assert.Equal(expectedY, position.GetProperty("y").GetInt32());
    }

    private void AssertOpaqueCompensationDraftRoundTrips(string json)
    {
        var activity = Assert.IsType<NotFoundActivity>(Serializer.Deserialize(json));
        Assert.Equal(CompensationScope.ActivityTypeName, activity.MissingTypeName);
        Assert.Equal(json, activity.OriginalActivityJson);

        AssertOpaqueSerializedCompensationDraft(Serializer, activity, json);
    }

    private static void AssertOpaqueSerializedCompensationDraft(IActivitySerializer serializer, NotFoundActivity activity, string json)
    {
        var serialized = serializer.Serialize(activity);
        using var document = JsonDocument.Parse(serialized);
        Assert.Equal(json, document.RootElement.GetProperty("originalActivityJson").GetString());

        var restored = Assert.IsType<NotFoundActivity>(serializer.Deserialize(serialized));
        Assert.Equal(json, restored.OriginalActivityJson);
    }

    private static string CreateCompensationDraftJson(string stepJson) =>
        $$"""{"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"onForwardFault":"Compensated","steps":[{{stepJson}}]}""";

    private static string CreateWriteLineJson(string id, int version) =>
        $$"""{"id":"{{id}}","type":"Elsa.WriteLine","version":{{version}}}""";

    private static string AddCaseAlias(string json, string propertyName, string aliasName, string canonicalValue, string aliasValue)
    {
        var canonicalProperty = $"{JsonSerializer.Serialize(propertyName)}:{canonicalValue}";
        var ambiguousProperty = $"{canonicalProperty},{JsonSerializer.Serialize(aliasName)}:{aliasValue}";
        var ambiguousJson = json.Replace(canonicalProperty, ambiguousProperty, StringComparison.Ordinal);
        Assert.NotEqual(json, ambiguousJson);
        return ambiguousJson;
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"future\"")]
    [InlineData("{}")]
    public void UnsupportedCompensationVersionPreservesRawDraftWithoutHydratingIt(string version)
    {
        var json = "{\"id\":\"scope-1\",\"type\":\"Elsa.CompensationScope\",\"version\":" + version +
                   ",\"draftSchemaVersion\":1,\"steps\":[],\"metadata\":{\"future\":[1,2]}}";

        var activity = Assert.IsType<NotFoundActivity>(Serializer.Deserialize(json));
        Assert.Equal(0, activity.MissingTypeVersion);
        Assert.Equal("scope-1", activity.Id);
        Assert.Equal(CompensationScope.ActivityTypeName, activity.MissingTypeName);
        Assert.Equal(json, activity.OriginalActivityJson);

        var restored = Assert.IsType<NotFoundActivity>(Serializer.Deserialize(Serializer.Serialize(activity)));
        Assert.Equal(json, restored.OriginalActivityJson);
    }

    [Fact]
    public void When_RoundtripSupportedCompensationDraft_Then_PreservesMappingsAndExtensionData()
    {
        const string template = """
            {"id":"scope-1","type":"Elsa.CompensationScope","version":1,"draftSchemaVersion":1,"scopeExtension":{"keep":[1,"two"]},"steps":[{"stepId":"step-1","forward":{"id":"forward-1","type":"WRITE_LINE_TYPE","version":1,"text":{"typeName":"String","expression":{"type":"Literal","value":"hello"}}},"handler":null,"mappings":[{"mappingId":"mapping-1","target":"targetId","opaque":{"keep":true}}],"rowExtension":{"keep":"row"}}]}
            """;
        var json = template.Replace("WRITE_LINE_TYPE", "Elsa.WriteLine", StringComparison.Ordinal);
        var activity = Assert.IsType<CompensationScope>(Serializer.Deserialize(json));
        Assert.IsType<WriteLine>(Assert.Single(activity.Steps).Forward);

        var roundtrip = Serializer.Serialize(activity);
        using var expected = JsonDocument.Parse(json);
        using var actual = JsonDocument.Parse(roundtrip);

        Assert.Equal(expected.RootElement.GetProperty("scopeExtension").GetRawText(), actual.RootElement.GetProperty("scopeExtension").GetRawText());
        Assert.Equal(expected.RootElement.GetProperty("steps")[0].GetProperty("mappings").GetRawText(), actual.RootElement.GetProperty("steps")[0].GetProperty("mappings").GetRawText());
        Assert.Equal(expected.RootElement.GetProperty("steps")[0].GetProperty("rowExtension").GetRawText(), actual.RootElement.GetProperty("steps")[0].GetProperty("rowExtension").GetRawText());
    }

    [Fact]
    public void CompensationDraftRoundTripsNativeStateMachineOwnedPins()
    {
        var machine = new StateMachine
        {
            Id = "machine-1",
            States = [new StateMachineState { Name = "state", Entry = new WriteLine("entry") { Id = "entry-1" }, Exit = new WriteLine("exit") { Id = "exit-1" } }],
            Transitions = [new Transition { From = "state", To = "state", Trigger = new WriteLine("trigger") { Id = "trigger-1" }, Action = new WriteLine("action") { Id = "action-1" } }]
        };
        var scope = new CompensationScope { Id = "scope-1", Steps = [new CompensationStep { StepId = "step-1", Forward = machine }] };

        var restoredScope = Assert.IsType<CompensationScope>(Serializer.Deserialize(Serializer.Serialize(scope)));
        var restoredMachine = Assert.IsType<StateMachine>(Assert.Single(restoredScope.Steps).Forward);
        var state = Assert.Single(restoredMachine.States);
        var transition = Assert.Single(restoredMachine.Transitions);
        Assert.Equal("entry-1", Assert.IsType<WriteLine>(state.Entry).Id);
        Assert.Equal("exit-1", Assert.IsType<WriteLine>(state.Exit).Id);
        Assert.Equal("trigger-1", Assert.IsType<WriteLine>(transition.Trigger).Id);
        Assert.Equal("action-1", Assert.IsType<WriteLine>(transition.Action).Id);
    }

    [Fact]
    public async Task CompensationDraftRoundTripsExactPinsInJsonIncludedPocoMembers()
    {
        await using var producer = await CreatePinTestServicesAsync(1);
        await using var consumer = await CreatePinTestServicesAsync(1);
        var serializer = producer.GetRequiredService<IActivitySerializer>();
        var json = serializer.Serialize(CreatePinTestScope(1, includeProperty: true, includeField: true, includeConditional: true));
        using var document = JsonDocument.Parse(json);
        var holder = document.RootElement.GetProperty("steps")[0].GetProperty("forward").GetProperty("holder");

        Assert.Equal("Test.VersionedNestedActivity", holder.GetProperty("privateGetterActivity").GetProperty("type").GetString());
        Assert.Equal(1, holder.GetProperty("privateGetterActivity").GetProperty("version").GetInt32());
        Assert.Equal("Test.VersionedNestedActivity", holder.GetProperty("fieldActivity").GetProperty("type").GetString());
        Assert.Equal(1, holder.GetProperty("fieldActivity").GetProperty("version").GetInt32());
        Assert.Equal("Test.VersionedNestedActivity", holder.GetProperty("conditionalActivity").GetProperty("type").GetString());
        Assert.Equal(1, holder.GetProperty("conditionalActivity").GetProperty("version").GetInt32());

        var restored = Assert.IsType<CompensationScope>(consumer.GetRequiredService<IActivitySerializer>().Deserialize(json));
        var restoredComposite = Assert.IsType<JsonIncludeActivityComposite>(Assert.Single(restored.Steps).Forward);
        Assert.Equal("property-child", restoredComposite.Holder.ReadPrivateGetterActivity()!.Id);
        Assert.Equal("field-child", restoredComposite.Holder.FieldActivity!.Id);
        Assert.Equal("conditional-child", restoredComposite.Holder.ConditionalActivity!.Id);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task CompensationDraftPreservesWholeDraftWhenJsonIncludedPocoMemberHasUnavailablePin(bool includeProperty, bool includeField, bool includeConditional)
    {
        await using var producer = await CreatePinTestServicesAsync(2);
        await using var consumer = await CreatePinTestServicesAsync(1);
        var json = producer.GetRequiredService<IActivitySerializer>()
            .Serialize(CreatePinTestScope(2, includeProperty, includeField, includeConditional));

        var restored = Assert.IsType<NotFoundActivity>(consumer.GetRequiredService<IActivitySerializer>().Deserialize(json));

        Assert.Equal(CompensationScope.ActivityTypeName, restored.MissingTypeName);
        Assert.Equal(json, restored.OriginalActivityJson);
        Assert.IsType<NotFoundActivity>(consumer.GetRequiredService<IActivitySerializer>().Deserialize(
            consumer.GetRequiredService<IActivitySerializer>().Serialize(restored)));
    }

    [Theory]
    [InlineData("holder", "HOLDER")]
    [InlineData("privateGetterActivity", "PRIVATEGETTERACTIVITY")]
    [InlineData("fieldActivity", "FIELDACTIVITY")]
    [InlineData("conditionalActivity", "CONDITIONALACTIVITY")]
    public async Task CompensationDraftPreservesWholeDraftWhenTypedOwnedMemberHasNoncanonicalCase(string propertyName, string aliasName)
    {
        await using var producer = await CreatePinTestServicesAsync(1);
        await using var consumer = await CreatePinTestServicesAsync(1);
        var json = producer.GetRequiredService<IActivitySerializer>()
            .Serialize(CreatePinTestScope(1, includeProperty: true, includeField: true, includeConditional: true));
        var aliasedJson = json.Replace($"\"{propertyName}\":", $"\"{aliasName}\":", StringComparison.Ordinal);
        Assert.NotEqual(json, aliasedJson);

        var serializer = consumer.GetRequiredService<IActivitySerializer>();
        var restored = Assert.IsType<NotFoundActivity>(serializer.Deserialize(aliasedJson));

        Assert.Equal(CompensationScope.ActivityTypeName, restored.MissingTypeName);
        Assert.Equal(aliasedJson, restored.OriginalActivityJson);
        AssertOpaqueSerializedCompensationDraft(serializer, restored, aliasedJson);
    }

    [Fact]
    public async Task CompensationDraftPreservesWholeDraftWhenOwnedMemberHasCaseAmbiguity()
    {
        await using var producer = await CreatePinTestServicesAsync(1);
        await using var consumer = await CreatePinTestServicesAsync(1);
        var json = producer.GetRequiredService<IActivitySerializer>()
            .Serialize(CreatePinTestScope(1, includeProperty: true, includeField: false));
        var ambiguousJson = json.Replace("\"privateGetterActivity\":", "\"PRIVATEGETTERACTIVITY\":null,\"privateGetterActivity\":", StringComparison.Ordinal);
        Assert.NotEqual(json, ambiguousJson);

        var serializer = consumer.GetRequiredService<IActivitySerializer>();
        var restored = Assert.IsType<NotFoundActivity>(serializer.Deserialize(ambiguousJson));

        Assert.Equal(CompensationScope.ActivityTypeName, restored.MissingTypeName);
        Assert.Equal(ambiguousJson, restored.OriginalActivityJson);
        AssertOpaqueSerializedCompensationDraft(serializer, restored, ambiguousJson);
    }

    [Fact]
    public void CompensationDraftChildPreservesClrSerializationMetadata()
    {
        var child = new GeneratedActivity
        {
            Id = "child-1", Type = "Test.Generated", Version = 2,
            Alias = "authored alias", Number = 7, RetainedNull = "authored retained value"
        };
        child.SyntheticProperties["Text"] = new Input<string>("authored text");
        var scope = new CompensationScope { Id = "scope-1", Steps = [new CompensationStep { StepId = "step-1", Forward = child }] };

        var restored = Assert.IsType<CompensationScope>(Serializer.Deserialize(Serializer.Serialize(scope)));
        var restoredChild = Assert.IsType<GeneratedActivity>(Assert.Single(restored.Steps).Forward);

        Assert.Equal(child.Alias, restoredChild.Alias);
        Assert.Equal(child.Number, restoredChild.Number);
        Assert.Equal(child.RetainedNull, restoredChild.RetainedNull);
    }

    [Fact]
    public void OrdinaryObjectsRetainTheirSerialization()
    {
        using var document = JsonDocument.Parse(Serializer.Serialize(new { Message = "hello", Count = 3 }));
        Assert.Equal("hello", document.RootElement.GetProperty("message").GetString());
        Assert.Equal(3, document.RootElement.GetProperty("count").GetInt32());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RootSerializationPreservesClrJsonMetadata(bool generated)
    {
        var activity = new GeneratedActivity { Id = "metadata", Type = generated ? "Test.Generated" : "Test.Unregistered", Version = 2 };
        if (generated)
        {
            activity.SyntheticProperties["Text"] = new Input<string>("synthetic");
        }
        using var document = JsonDocument.Parse(Serializer.Serialize(activity));
        Assert.Equal("named", document.RootElement.GetProperty("external_name").GetString());
        Assert.False(document.RootElement.TryGetProperty("alias", out _));
        Assert.False(document.RootElement.TryGetProperty("omitted", out _));
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("retainedNull").ValueKind);
        Assert.Equal(42, document.RootElement.GetProperty("number").GetInt32());
    }

    [Fact]
    public void DescriptorConverterDoesNotLeakIntoLaterDeserializationOrDuplicateSyntheticInputs()
    {
        var registry = _services.GetRequiredService<IActivityRegistry>();
        var descriptor = registry.Find("Test.Generated", 2)!;
        descriptor.ConfigureSerializerOptions = options =>
        {
            options.Converters.Insert(0, new JsonIgnoreCompositeRootConverterFactory(_services.GetRequiredService<ActivityWriter>()));
            return options;
        };
        var activity = new GeneratedActivity { Id = "configured", Type = descriptor.TypeName, Version = descriptor.Version };
        activity.SyntheticProperties["Text"] = new Input<string>("configured value");
        var serializer = Serializer;
        var json = serializer.Serialize(activity);
        using var document = JsonDocument.Parse(json);
        Assert.Single(document.RootElement.EnumerateObject(), property => property.Name == "text");
        var restored = Assert.IsType<GeneratedActivity>(serializer.Deserialize(json));
        Assert.IsType<Input<string>>(restored.SyntheticProperties["Text"]);
        Assert.IsType<WriteLine>(serializer.Deserialize("{\"type\":\"Elsa.WriteLine\",\"version\":1,\"id\":\"later\"}"));
    }

    [Fact]
    public void GeneratedRootHonorsConfiguredDepthDuringValidation()
    {
        var descriptor = _services.GetRequiredService<IActivityRegistry>().Find("Test.Generated", 2)!;
        descriptor.ConfigureSerializerOptions = options =>
        {
            options.MaxDepth = 128;
            return options;
        };
        descriptor.Inputs.Single().Type = typeof(object);
        object value = "deep value";
        for (var depth = 0; depth < 70; depth++)
        {
            value = new Dictionary<string, object> { ["child"] = value };
        }
        var activity = new GeneratedActivity { Type = descriptor.TypeName, Version = descriptor.Version };
        activity.SyntheticProperties["Text"] = new Input<object>(value);
        using var document = JsonDocument.Parse(Serializer.Serialize(activity), new JsonDocumentOptions { MaxDepth = 128 });
        Assert.True(document.RootElement.TryGetProperty("text", out _));
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("ID")]
    [InlineData("ExternalName")]
    [InlineData("result")]
    public void RootRejectsAmbiguousSyntheticPropertyNames(string name)
    {
        var descriptor = _services.GetRequiredService<IActivityRegistry>().Find("Test.Generated", 2)!;
        descriptor.Inputs.Add(new InputDescriptor { Name = name, Type = typeof(string), IsSynthetic = true });
        var activity = new GeneratedActivity { Id = "collision", Type = descriptor.TypeName, Version = descriptor.Version };
        activity.SyntheticProperties[name] = new Input<string>("collision");
        activity.SyntheticProperties["Result"] = new Output<string>(new Variable("result"));
        Assert.Throws<JsonException>(() => Serializer.Serialize(activity));
    }

    public sealed class GeneratedActivity : Activity
    {
        [JsonPropertyName("external_name")]
        public string Alias { get; set; } = "named";

        [JsonPropertyName("externalName")]
        public string CollisionAlias { get; set; } = "alias";

        public string? Omitted { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ConditionalNull { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? RetainedNull { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Number { get; set; } = 42;
    }

    private static async Task<ServiceProvider> CreatePinTestServicesAsync(int nestedActivityVersion)
    {
        var services = new ServiceCollection();
        services.AddElsa();
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IActivityRegistry>();
        await registry.RegisterAsync<CompensationScope>();
        await registry.RegisterAsync<Sequence>();
        registry.Register(new ActivityDescriptor
        {
            TypeName = "Test.JsonIncludeActivityComposite",
            ClrType = typeof(JsonIncludeActivityComposite),
            Version = 1,
            Constructor = context => context.CreateActivity<JsonIncludeActivityComposite>()
        });
        registry.Register(new ActivityDescriptor
        {
            TypeName = "Test.VersionedNestedActivity",
            ClrType = typeof(VersionedNestedActivity),
            Version = nestedActivityVersion,
            Constructor = context => context.CreateActivity<VersionedNestedActivity>()
        });
        return serviceProvider;
    }

    private static CompensationScope CreatePinTestScope(int nestedActivityVersion, bool includeProperty, bool includeField, bool includeConditional = false)
    {
        var holder = new JsonIncludeActivityHolder();
        if (includeProperty)
            holder.SetPrivateGetterActivity(CreateVersionedNestedActivity("property-child", nestedActivityVersion));
        if (includeField)
            holder.FieldActivity = CreateVersionedNestedActivity("field-child", nestedActivityVersion);
        if (includeConditional)
            holder.ConditionalActivity = CreateVersionedNestedActivity("conditional-child", nestedActivityVersion);

        return new CompensationScope
        {
            Id = "scope-1",
            Steps =
            [
                new CompensationStep
                {
                    StepId = "step-1",
                    Forward = new JsonIncludeActivityComposite
                    {
                        Id = "composite-1",
                        Type = "Test.JsonIncludeActivityComposite",
                        Version = 1,
                        Holder = holder
                    }
                }
            ]
        };
    }

    private static VersionedNestedActivity CreateVersionedNestedActivity(string id, int version) => new()
    {
        Id = id,
        Type = "Test.VersionedNestedActivity",
        Version = version
    };

    public sealed class JsonIncludeActivityComposite : Composite
    {
        public JsonIncludeActivityHolder Holder { get; set; } = new();
    }

    public sealed class JsonIncludeActivityHolder
    {
        [JsonInclude]
        public IActivity? PrivateGetterActivity { private get; set; }

        [JsonInclude]
        public IActivity? FieldActivity = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IActivity? ConditionalActivity { get; set; }

        public void SetPrivateGetterActivity(IActivity activity) => PrivateGetterActivity = activity;

        public IActivity? ReadPrivateGetterActivity() => PrivateGetterActivity;
    }

    public sealed class VersionedNestedActivity : Activity
    {
    }
}
