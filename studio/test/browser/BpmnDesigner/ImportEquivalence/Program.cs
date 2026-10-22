// Usage: dotnet run -- <round-trip directory> <repository root>
//
// The round-trip directory is what the suite's preservation tests write with BPMN_DESIGNER_OUTPUT set: per document, a
// manifest (<name>.json) naming the original and the load warnings, and the saved copy. Exit code 0 means Elsa imports
// every saved copy exactly as it imports the original, except where the load reported dropped content.
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: BpmnImportEquivalence <round-trip directory> <repository root>");
    return 2;
}

var roundTrips = args[0];
var repositoryRoot = args[1];
var manifests = Directory.GetFiles(roundTrips, "*.json").Order(StringComparer.Ordinal).ToList();
if (manifests.Count == 0)
{
    Console.Error.WriteLine($"No round trips in '{roundTrips}'. Run the suite with BPMN_DESIGNER_OUTPUT set to it first.");
    return 1;
}

var services = new TestApplicationBuilder(new NoOutput()).ConfigureElsa(elsa => elsa.UseBpmnInterchange()).Build();
await services.PopulateRegistriesAsync();
var documents = services.GetRequiredService<BpmnInterchangeDocumentService>();

var failures = 0;
foreach (var manifestPath in manifests)
{
    var manifest = JsonNode.Parse(await File.ReadAllTextAsync(manifestPath))!.AsObject();
    var name = manifest["name"]!.GetValue<string>();
    var original = await File.ReadAllTextAsync(Path.Combine(repositoryRoot, manifest["source"]!.GetValue<string>()));
    var saved = await File.ReadAllTextAsync(Path.Combine(roundTrips, manifest["saved"]!.GetValue<string>()));
    // A host treats a warning kind it does not know as dropped.
    var reportedLoss = manifest["warnings"]!.AsArray().Any(warning => warning!["kind"]!.GetValue<string>() != "rewritten");

    var before = await ImportAsync(original);
    var after = await ImportAsync(saved);
    var differences = Differences(before, after);

    if (differences.Count == 0)
    {
        Console.WriteLine($"same       {name}");
    }
    else if (reportedLoss)
    {
        Console.WriteLine($"differs    {name} (its load reported dropped content): {string.Join(", ", differences)}");
    }
    else
    {
        failures++;
        Console.WriteLine($"DIFFERENT  {name}: {string.Join(", ", differences)}");
        foreach (var property in differences)
        {
            Console.WriteLine($"  {property} of the original:   {before[property]?.ToJsonString()}");
            Console.WriteLine($"  {property} of the saved copy: {after[property]?.ToJsonString()}");
        }
    }
}

Console.WriteLine(failures == 0
    ? $"Elsa imports all {manifests.Count} saved copies as it imports their originals, except where a load reported dropped content."
    : $"Elsa imports {failures} of {manifests.Count} saved copies differently from their originals, with no dropped content reported.");
return failures == 0 ? 0 : 1;

// What Elsa makes of a document: whether it imports, the workflow it builds and what its analysis reports. The stored
// source XML and its hash differ by construction, so they are left out.
async Task<JsonObject> ImportAsync(string xml)
{
    try
    {
        var result = await documents.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        var definition = result.ImportResult.WorkflowDefinition;
        return new JsonObject
        {
            ["succeeded"] = result.ImportResult.Succeeded,
            ["validationErrors"] = new JsonArray(result.ImportResult.ValidationErrors.Select(error => (JsonNode)JsonValue.Create(error.Message)).ToArray()),
            ["root"] = JsonNode.Parse(definition.StringData ?? "null"),
            ["customProperties"] = new JsonArray(definition.CustomProperties.Keys
                .Where(key => key is not ("Bpmn:SourceXml" or "Bpmn:SourceGraphHash"))
                .Order(StringComparer.Ordinal)
                .Select(key => (JsonNode)JsonValue.Create($"{key}={JsonSerializer.Serialize(definition.CustomProperties[key])}"))
                .ToArray()),
            ["issues"] = new JsonArray(result.Analysis.Issues.Select(issue => (JsonNode)JsonValue.Create($"{issue.Severity} {issue.ElementId}: {issue.Message}")).ToArray()),
            ["elementCounts"] = JsonSerializer.SerializeToNode(result.Analysis.ElementCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary())
        };
    }
    catch (Exception exception) when (IsRefusal(exception))
    {
        return new JsonObject { ["refused"] = $"{exception.GetType().Name}: {exception.Message}" };
    }
}

// The import refuses a document by throwing one of its own exceptions, or one of the Bpmn.* libraries it reads documents
// with. Anything else is a fault in the import itself: it fails the check instead of comparing equal to the same fault.
static bool IsRefusal(Exception exception) =>
    exception.GetType().Namespace is { } name && (name.StartsWith("Elsa.Bpmn", StringComparison.Ordinal) || name.StartsWith("Bpmn.", StringComparison.Ordinal));

static List<string> Differences(JsonObject before, JsonObject after) =>
    before.Select(property => property.Key)
        .Union(after.Select(property => property.Key))
        .Where(key => !JsonNode.DeepEquals(before[key], after[key]))
        .ToList();

internal sealed class NoOutput : ITestOutputHelper
{
    public void WriteLine(string message)
    {
    }

    public void WriteLine(string format, params object[] args)
    {
    }
}
