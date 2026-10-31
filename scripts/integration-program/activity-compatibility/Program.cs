using System.Reflection;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

if (args.Length != 3)
{
    throw new ArgumentException("Expected assembly-list, output path, and optional baseline workflow path ('-' for export).");
}

var outputPath = Path.GetFullPath(args[1]);
if (File.Exists(outputPath))
{
    throw new InvalidOperationException("Refusing to overwrite existing evidence.");
}

var assemblyNames = args[0].Split(',', StringSplitOptions.RemoveEmptyEntries).Order().ToArray();
var assemblies = assemblyNames.Select(Assembly.Load).ToArray();
var types = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
    .Where(type => typeof(IActivity).IsAssignableFrom(type))
    .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
var services = new ServiceCollection();
services.AddElsa();
await using var provider = services.BuildServiceProvider();
var describer = provider.GetRequiredService<IActivityDescriber>();
var registry = provider.GetRequiredService<IActivityRegistry>();
var serializer = provider.GetRequiredService<IActivitySerializer>();
var descriptors = new List<object>();
var exclusions = new List<object>();
var failures = new List<object>();
var firstWriteActivities = new List<IActivity>();
var activityIdentities = new HashSet<(string, int)>();
var roundTrips = new List<object>();
await registry.RegisterAsync(typeof(Sequence));
var candidates = types.Select(type => (Type: type, Descriptor: (ActivityDescriptor?)null, Instance: (IActivity?)null, Provider: (string?)null)).ToList();
foreach (var generated in await GeneratedActivityFixtures.DescribeAsync(provider))
{
    var context = new ActivityConstructorContext(generated.Descriptor, type => new ActivityConstructionResult(CreateDefaultActivity(type)));
    var activity = generated.Descriptor.Constructor(context).Activity;
    candidates.Add((activity.GetType(), generated.Descriptor, activity, generated.Provider));
}
foreach (var candidate in candidates)
{
    var type = candidate.Type;
    if (type.IsAbstract || type.ContainsGenericParameters)
    {
        exclusions.Add(new { ClrType = type.FullName, Reason = type.IsAbstract ? "abstract" : "open-generic-requires-host-configuration" });
        continue;
    }

    try
    {
        var descriptor = candidate.Descriptor ?? await describer.DescribeActivityAsync(type);
        if (!activityIdentities.Add((descriptor.TypeName, descriptor.Version)))
        {
            throw new InvalidOperationException($"Duplicate activity identity: {descriptor.TypeName} v{descriptor.Version}.");
        }
        registry.Register(descriptor);
        descriptors.Add(new
        {
            Assembly = type.Assembly.GetName().Name,
            candidate.Provider,
            ClrType = type.FullName,
            descriptor.TypeName,
            descriptor.Version,
            Kind = descriptor.Kind.ToString(),
            Inputs = descriptor.Inputs.OrderBy(input => input.Name, StringComparer.Ordinal)
                .Select(input => new { input.Name, Type = input.Type.FullName, ContractType = TypeIdentity(input.Type), input.IsSerializable }).ToArray(),
            Outputs = descriptor.Outputs.OrderBy(output => output.Name, StringComparer.Ordinal)
                .Select(output => new { output.Name, Type = output.Type.FullName, ContractType = TypeIdentity(output.Type), output.IsSerializable }).ToArray(),
            Ports = descriptor.Ports.OrderBy(port => port.Name, StringComparer.Ordinal)
                .Select(port => new { port.Name, Type = port.Type.ToString() }).ToArray()
        });

        var constructor = FindDefaultConstructor(type);
        if (constructor is null && candidate.Instance is null)
        {
            exclusions.Add(new { ClrType = type.FullName, Reason = "constructor-requires-services-or-inputs" });
            continue;
        }

        var activity = candidate.Instance ?? CreateDefaultActivity(type);
        activity.Id = $"compatibility-{firstWriteActivities.Count + 1}";
        activity.Type = descriptor.TypeName;
        activity.Version = descriptor.Version;
        foreach (var property in type.GetProperties().Where(property => property.CanWrite))
        {
            if (property.PropertyType == typeof(Input<string>))
            {
                property.SetValue(activity, new Input<string>("compatibility-probe"));
            }
            else if (property.PropertyType == typeof(Input<int>))
            {
                property.SetValue(activity, new Input<int>(42));
            }
            else if (property.PropertyType == typeof(Input<List<string>>))
            {
                property.SetValue(activity, new Input<List<string>>(new List<string> { "compatibility-probe" }));
            }
            else if (property.PropertyType == typeof(Input<bool>))
            {
                property.SetValue(activity, new Input<bool>(true));
            }
        }
        foreach (var input in descriptor.Inputs.Where(input => input.IsSynthetic))
        {
            if (input.Type == typeof(string))
            {
                input.ValueSetter(activity, new Input<string>("compatibility-probe"));
            }
            else if (input.Type == typeof(int))
            {
                input.ValueSetter(activity, new Input<int>(42));
            }
        }
        if (candidate.Provider is not null && activity is Elsa.ServiceBus.MassTransit.Activities.PublishMessage message)
        {
            message.Message = new Input<object>(new CompatibilityMessage("compatibility-probe"));
        }
        var serialized = serializer.Serialize(activity);
        try
        {
            var restoredActivity = serializer.Deserialize(serialized);
            var literalValues = await LiteralValues(activity, provider);
            var restoredLiteralValues = await LiteralValues(restoredActivity, provider);
            var identityPreserved = restoredActivity.Id == activity.Id && restoredActivity.Type == activity.Type && restoredActivity.Version == activity.Version && restoredActivity.GetType() == type;
            var roundTrip = serializer.Serialize(restoredActivity);
            var stableRoundTrip = serializer.Serialize(serializer.Deserialize(roundTrip));
            var semanticSerialized = NormalizeLiterals(serialized, type, literalValues);
            var semanticRoundTrip = NormalizeLiterals(roundTrip, type, restoredLiteralValues);
            var literalValuesPreserved = JsonEqual(JsonSerializer.Serialize(literalValues), JsonSerializer.Serialize(restoredLiteralValues));
            var preserved = identityPreserved && literalValuesPreserved && AllowsDefaultEnrichment(semanticSerialized, semanticRoundTrip) && JsonEqual(roundTrip, stableRoundTrip);
            roundTrips.Add(new { ClrType = type.FullName, descriptor.TypeName, descriptor.Version, candidate.Provider, Serialized = serialized, RoundTrip = roundTrip, IdentityPreserved = identityPreserved, LiteralValues = literalValues, RestoredLiteralValues = restoredLiteralValues, LiteralValuesPreserved = literalValuesPreserved, Preserved = preserved });
            if (identityPreserved)
            {
                firstWriteActivities.Add(activity);
            }
            if (!preserved)
            {
                failures.Add(new { ClrType = type.FullName, descriptor.TypeName, descriptor.Version, candidate.Provider, Stage = "individual-round-trip", ErrorType = "JsonMismatch", Message = "Serialized activity changed during same-host round-trip." });
            }
        }
        catch (Exception exception)
        {
            failures.Add(new { ClrType = type.FullName, descriptor.TypeName, descriptor.Version, candidate.Provider, Stage = "individual-round-trip", Serialized = serialized, ErrorType = exception.GetType().FullName, exception.Message });
        }
    }
    catch (Exception exception)
    {
        failures.Add(new { ClrType = type.FullName, ErrorType = exception.GetType().FullName, exception.Message });
    }
}

var workflow = CreateWorkflow(firstWriteActivities);
var firstWriteWorkflow = serializer.Serialize(workflow);
var originalWorkflow = firstWriteWorkflow;
var serializedWorkflow = originalWorkflow;
var selfRoundTrip = string.Empty;
var selfPreserved = false;
try
{
    var restoredWorkflow = (Sequence)serializer.Deserialize(originalWorkflow);
    serializedWorkflow = serializer.Serialize(restoredWorkflow);
    selfRoundTrip = serializer.Serialize(serializer.Deserialize(serializedWorkflow));
    var originalContracts = await WorkflowContracts(firstWriteActivities, provider);
    var restoredContracts = await WorkflowContracts(restoredWorkflow.Activities, provider);
    var semanticOriginal = await NormalizeWorkflow(originalWorkflow, firstWriteActivities, provider);
    var semanticRestored = await NormalizeWorkflow(serializedWorkflow, restoredWorkflow.Activities, provider);
    selfPreserved = JsonEqual(JsonSerializer.Serialize(originalContracts), JsonSerializer.Serialize(restoredContracts))
        && AllowsDefaultEnrichment(semanticOriginal, semanticRestored) && JsonEqual(serializedWorkflow, selfRoundTrip);
}
catch (Exception exception)
{
    failures.Add(new { ClrType = typeof(Sequence).FullName, Stage = "workflow-round-trip", ErrorType = exception.GetType().FullName, exception.Message });
}
var workflowContracts = await WorkflowContracts(firstWriteActivities, provider);
string? importedRoundTrip = null;
bool? importedContractsPreserved = null;
bool? importedFirstWritePreserved = null;
bool? importedPreserved = null;
if (args[2] != "-")
{
    using var baseline = JsonDocument.Parse(await File.ReadAllTextAsync(args[2]));
    var historicalWorkflow = baseline.RootElement.GetProperty("serializedWorkflow").GetString()!;
    try
    {
        var imported = serializer.Deserialize(historicalWorkflow);
        importedRoundTrip = serializer.Serialize(imported);
        if (imported is not Sequence importedSequence)
        {
            throw new InvalidOperationException("Historical root did not resolve to Sequence.");
        }
        var importedContracts = await WorkflowContracts(importedSequence.Activities, provider);
        importedContractsPreserved = JsonEqual(baseline.RootElement.GetProperty("workflowContracts").GetRawText(), JsonSerializer.Serialize(importedContracts));
        var firstWrite = serializer.Deserialize(baseline.RootElement.GetProperty("firstWriteWorkflow").GetString()!);
        if (firstWrite is not Sequence firstWriteSequence)
        {
            throw new InvalidOperationException("First-write historical root did not resolve to Sequence.");
        }
        var firstWriteContracts = await WorkflowContracts(firstWriteSequence.Activities, provider);
        importedFirstWritePreserved = JsonEqual(historicalWorkflow, serializer.Serialize(firstWrite)) && JsonEqual(baseline.RootElement.GetProperty("workflowContracts").GetRawText(), JsonSerializer.Serialize(firstWriteContracts));
        importedPreserved = JsonEqual(historicalWorkflow, importedRoundTrip) && importedContractsPreserved == true && importedFirstWritePreserved == true;
    }
    catch (Exception exception)
    {
        importedPreserved = false;
        failures.Add(new { ClrType = typeof(Sequence).FullName, Stage = "historical-workflow-import", ErrorType = exception.GetType().FullName, exception.Message });
    }
}

await using (var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
{
    await JsonSerializer.SerializeAsync(output, new
    {
        assemblies = assemblies.Select(assembly => new { name = assembly.GetName().Name, version = assembly.GetName().Version?.ToString(), path = assembly.Location }),
        descriptors,
        exclusions,
        failures,
        roundTrips,
        serializedActivities = firstWriteActivities.Count,
        workflowContracts,
        importedContractsPreserved,
        importedFirstWritePreserved,
        firstWriteWorkflow,
        originalWorkflow,
        serializedWorkflow,
        selfRoundTrip,
        selfPreserved,
        importedRoundTrip,
        importedPreserved,
        executedActivities = 0
    }, new JsonSerializerOptions { WriteIndented = true });
}
Console.WriteLine($"Descriptors={descriptors.Count}; serialized activities={firstWriteActivities.Count}; exclusions={exclusions.Count}; failures={failures.Count}; self round-trip={selfPreserved}; historical round-trip={importedPreserved}");
return failures.Count == 0 && selfPreserved && importedPreserved is not false ? 0 : 1;

static bool JsonEqual(string left, string right)
{
    return System.Text.Json.Nodes.JsonNode.DeepEquals(System.Text.Json.Nodes.JsonNode.Parse(left), System.Text.Json.Nodes.JsonNode.Parse(right));
}

// The released serializer materializes these two absent false defaults. Preserve
// every supplied field/value and reject every other addition/removal/change.
static bool AllowsDefaultEnrichment(string original, string normalized)
{
    var left = System.Text.Json.Nodes.JsonNode.Parse(original);
    var right = System.Text.Json.Nodes.JsonNode.Parse(normalized);
    return Compare(left, right, false);

    static bool Compare(System.Text.Json.Nodes.JsonNode? left, System.Text.Json.Nodes.JsonNode? right, bool customProperties)
    {
        if (left is System.Text.Json.Nodes.JsonObject a && right is System.Text.Json.Nodes.JsonObject b)
        {
            foreach (var pair in a)
            {
                if (!b.TryGetPropertyValue(pair.Key, out var value) || !Compare(pair.Value, value, pair.Key == "customProperties"))
                {
                    return false;
                }
            }
            return b.All(pair => a.ContainsKey(pair.Key) ||
                customProperties && pair.Key is ("canStartWorkflow" or "runAsynchronously") && pair.Value is System.Text.Json.Nodes.JsonValue value && value.TryGetValue<bool>(out var flag) && !flag);
        }
        if (left is System.Text.Json.Nodes.JsonArray aa && right is System.Text.Json.Nodes.JsonArray bb)
        {
            return aa.Count == bb.Count && aa.Zip(bb).All(pair => Compare(pair.First, pair.Second, false));
        }
        return System.Text.Json.Nodes.JsonNode.DeepEquals(left, right);
    }
}

static async Task<SortedDictionary<string, JsonElement>> LiteralValues(IActivity activity, IServiceProvider provider)
{
    var values = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
    var evaluator = provider.GetRequiredService<IExpressionEvaluator>();
    foreach (var property in activity.GetType().GetProperties().Where(property => typeof(Input).IsAssignableFrom(property.PropertyType)))
    {
        if (property.GetValue(activity) is not Input { Expression: { Type: "Literal" } expression } input)
        {
            continue;
        }
        var context = new ExpressionExecutionContext(provider, new MemoryRegister());
        var value = await evaluator.EvaluateAsync(expression, input.Type, context);
        var targetType = input.Type;
        // PublishMessage executes this exact conversion before sending. Evaluate
        // it offline to compare its typed payload without invoking a bus.
        if (activity is Elsa.ServiceBus.MassTransit.Activities.PublishMessage { MessageType: not null } message && property.Name == nameof(message.Message))
        {
            targetType = message.MessageType;
            value = value.ConvertTo(targetType);
        }
        values.Add(property.Name, value is Type type ? JsonSerializer.SerializeToElement(TypeIdentity(type)) : JsonSerializer.SerializeToElement(value, targetType));
    }
    var descriptor = provider.GetRequiredService<IActivityRegistry>().Find(activity.Type, activity.Version);
    foreach (var inputDescriptor in descriptor?.Inputs.Where(input => input.IsSynthetic) ?? [])
    {
        if (inputDescriptor.ValueGetter(activity) is not Input { Expression: { Type: "Literal" } expression } input)
        {
            continue;
        }
        var value = await evaluator.EvaluateAsync(expression, input.Type, new ExpressionExecutionContext(provider, new MemoryRegister()));
        values.Add(inputDescriptor.Name, value is Type type ? JsonSerializer.SerializeToElement(TypeIdentity(type)) : JsonSerializer.SerializeToElement(value, input.Type));
    }
    return values;
}

// Assembly versions are captured separately. Constructed generic FullName embeds
// dependency build versions, which must not be confused with a changed CLR type.
static string TypeIdentity(Type type)
{
    if (type.IsArray)
    {
        return $"{TypeIdentity(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
    }
    if (type.IsGenericType)
    {
        return $"{type.GetGenericTypeDefinition().FullName}<{string.Join(",", type.GetGenericArguments().Select(TypeIdentity))}>, {AssemblyIdentity(type.Assembly)}";
    }
    return $"{type.FullName}, {AssemblyIdentity(type.Assembly)}";
}

// Normalize only known Literal input expression values after actual typed
// evaluation. Every surrounding property, expression type and metadata remains
// in the strict JSON comparison. No activity or non-literal expression executes.
static string NormalizeLiterals(string json, Type activityType, SortedDictionary<string, JsonElement> values)
{
    var root = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
    foreach (var pair in values)
    {
        var property = activityType.GetProperties().SingleOrDefault(property => property.Name == pair.Key && typeof(Input).IsAssignableFrom(property.PropertyType));
        var name = property?.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(pair.Key);
        if (root[name] is System.Text.Json.Nodes.JsonObject input && input["expression"] is System.Text.Json.Nodes.JsonObject expression && expression["type"]?.GetValue<string>() == "Literal")
        {
            expression["value"] = System.Text.Json.Nodes.JsonNode.Parse(pair.Value.GetRawText());
        }
    }
    return root.ToJsonString();
}

static async Task<List<object>> WorkflowContracts(IEnumerable<IActivity> activities, IServiceProvider provider)
{
    var contracts = new List<object>();
    foreach (var activity in activities)
    {
        contracts.Add(new { activity.Id, activity.Type, activity.Version, ClrType = TypeIdentity(activity.GetType()), Literals = await LiteralValues(activity, provider) });
    }
    return contracts;
}

static string AssemblyIdentity(Assembly assembly)
{
    var name = assembly.GetName();
    var token = name.GetPublicKeyToken();
    var tokenText = token is { Length: > 0 } ? Convert.ToHexString(token).ToLowerInvariant() : "null";
    return $"{name.Name}, Culture={ (string.IsNullOrEmpty(name.CultureName) ? "neutral" : name.CultureName)}, PublicKeyToken={tokenText}";
}

static Sequence CreateWorkflow(List<IActivity> activities) => new() { Id = "compatibility-root", Activities = activities };

static ConstructorInfo? FindDefaultConstructor(Type type) => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    .Where(candidate => candidate.IsPublic || candidate.GetCustomAttribute<System.Text.Json.Serialization.JsonConstructorAttribute>() is not null)
    .FirstOrDefault(candidate => candidate.GetParameters().All(parameter => parameter.HasDefaultValue));

static IActivity CreateDefaultActivity(Type type)
{
    var constructor = FindDefaultConstructor(type)
        ?? throw new InvalidOperationException($"No supported constructor for {type.FullName}.");
    return (IActivity)constructor.Invoke(constructor.GetParameters().Select(parameter => parameter.DefaultValue).ToArray());
}

static async Task<string> NormalizeWorkflow(string json, IEnumerable<IActivity> activities, IServiceProvider provider)
{
    var root = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
    var nodes = root["activities"]!.AsArray();
    var instances = activities.ToArray();
    if (nodes.Count != instances.Length)
    {
        throw new InvalidOperationException("Workflow JSON/activity counts differ.");
    }
    for (var index = 0; index < instances.Length; index++)
    {
        var activity = instances[index];
        var values = await LiteralValues(activity, provider);
        nodes[index] = System.Text.Json.Nodes.JsonNode.Parse(NormalizeLiterals(nodes[index]!.ToJsonString(), activity.GetType(), values));
    }
    return root.ToJsonString();
}
