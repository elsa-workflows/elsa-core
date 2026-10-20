// SPIKE (#8901): does Elsa build the same workflow from a document and from its bpmn-js round-tripped copy?
// Usage: dotnet run -- <spike output dir> <repository root>
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

var outDir = args[0];
var repo = args[1];
var sources = new Dictionary<string, string>
{
    ["core-assets"] = Path.Combine(repo, "core/test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets"),
    ["clientlib-fixtures"] = Path.Combine(repo, "studio/src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/bpmn/__fixtures__"),
    ["spike"] = Path.Combine(repo, "studio/spikes/bpmn-js-modeler/fixtures")
};

var services = new TestApplicationBuilder(new SilentOutput()).ConfigureElsa(elsa => elsa.UseBpmnInterchange()).Build();
await services.PopulateRegistriesAsync();
var documentService = services.GetRequiredService<BpmnInterchangeDocumentService>();

var report = new JsonArray();
foreach (var file in Directory.GetFiles(outDir, "*roundtrip-*.bpmn").Order())
{
    var name = Path.GetFileName(file);
    var key = name[(name.IndexOf("roundtrip-", StringComparison.Ordinal) + "roundtrip-".Length)..];
    var parts = key == "edited-crafted.bpmn" ? ["spike", "foreign-unknown-deep.bpmn"] : key.Split("__");
    var original = await File.ReadAllTextAsync(Path.Combine(sources[parts[0]], parts[1]));
    var roundTripped = await File.ReadAllTextAsync(file);

    var before = await ImportAsync(original);
    var after = await ImportAsync(roundTripped);
    var same = JsonNode.DeepEquals(before, after);
    var entry = new JsonObject
    {
        ["file"] = name,
        ["sameOutcome"] = same,
        ["original"] = Summary(before),
        ["roundTripped"] = Summary(after)
    };
    if (!same) entry["differences"] = Differences(before, after);
    report.Add(entry);
    Console.WriteLine($"{(same ? "SAME   " : "DIFFERS")} {name}: {Summary(before)}");
}

await File.WriteAllTextAsync(Path.Combine(outDir, "elsa-import-equivalence.json"), report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
return;

async Task<JsonObject> ImportAsync(string xml)
{
    try
    {
        var result = await documentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        var definition = result.ImportResult.WorkflowDefinition;
        return new JsonObject
        {
            ["succeeded"] = result.ImportResult.Succeeded,
            ["validationErrors"] = new JsonArray(result.ImportResult.ValidationErrors.Select(e => (JsonNode)JsonValue.Create(e.Message)!).ToArray()),
            ["root"] = JsonNode.Parse(definition.StringData ?? "null"),
            ["customProperties"] = new JsonArray(definition.CustomProperties.Keys.Where(k => k != "Bpmn:SourceXml").Order().Select(k => (JsonNode)JsonValue.Create($"{k}={(k == "Bpmn:SourceGraphHash" ? "(hash)" : definition.CustomProperties[k])}")!).ToArray()),
            ["issues"] = new JsonArray(result.Analysis.Issues.Select(i => (JsonNode)JsonValue.Create($"{i.Severity} {i.ElementId}: {i.Message}")!).ToArray()),
            ["elementCounts"] = JsonSerializer.SerializeToNode(result.Analysis.ElementCounts.OrderBy(p => p.Key).ToDictionary())
        };
    }
    catch (Exception exception)
    {
        return new JsonObject { ["refused"] = $"{exception.GetType().Name}: {exception.Message}" };
    }
}

static string Summary(JsonObject outcome) =>
    outcome["refused"] is { } refused
        ? $"refused ({refused.GetValue<string>().Split('\n')[0]})"
        : $"imported, {outcome["issues"]!.AsArray().Count} analysis issue(s)";

static JsonArray Differences(JsonObject before, JsonObject after)
{
    var differences = new JsonArray();
    foreach (var key in before.Select(p => p.Key).Union(after.Select(p => p.Key)))
    {
        if (!JsonNode.DeepEquals(before[key], after[key]))
            differences.Add(new JsonObject { ["property"] = key, ["original"] = before[key]?.DeepClone(), ["roundTripped"] = after[key]?.DeepClone() });
    }
    return differences;
}

internal sealed class SilentOutput : ITestOutputHelper
{
    public void WriteLine(string message) { }
    public void WriteLine(string format, params object[] args) { }
}
