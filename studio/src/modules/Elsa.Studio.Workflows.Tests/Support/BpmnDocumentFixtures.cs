using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// The BPMN document and activity fixtures of <c>camunda-order-process.bpmn</c>: a Camunda-authored process whose one
/// service task, <see cref="TaskId"/>, carries both <c>camunda:*</c> extensions and an <c>elsa:activityBinding</c>.
/// </summary>
/// <remarks>
/// <c>Fixtures/camunda-order-process.document.json</c> is not hand-written: it is
/// <c>Bpmn.Interchange</c> 0.2.0's <c>BpmnXmlReader.Read(xml).Definitions</c> for the Designer ClientLib's
/// <c>src/bpmn/__fixtures__/camunda-order-process.bpmn</c>, serialized with plain <c>System.Text.Json</c> defaults — exactly
/// what elsa-core's document <c>GET</c> sends. Regenerate it the same way after a <c>Bpmn.Model</c> version bump.
/// </remarks>
internal static class BpmnDocumentFixtures
{
    public const string DefinitionId = "order-definition";
    public const string ProcessId = "order-process";
    public const string TaskId = "NotifyWarehouse";
    public const string BoundActivityId = "order-process:node-NotifyWarehouse";

    /// <summary>The document <c>GET</c> body for the fixture.</summary>
    public static JsonObject Document() => Load("Fixtures", "camunda-order-process.document.json");

    /// <summary>The root <c>Elsa.BpmnProcess</c> activity elsa-core imports the fixture as.</summary>
    public static JsonObject RootActivity() => Load("DesignerAssets", "camunda-order-process.activity.json");

    /// <summary>The fixture's task, in <paramref name="document"/>.</summary>
    public static JsonObject TaskElement(JsonObject document) => (JsonObject)document["processes"]![0]!["elements"]![1]!;

    /// <summary>A descriptor for <c>Elsa.WriteLine</c>, whose one input is the wrapped <c>Text</c>.</summary>
    public static ActivityDescriptor WriteLine() => Descriptor("Elsa.WriteLine", "Write Line", Input("Text", "String", isWrapped: true));

    public static ActivityDescriptor Descriptor(string typeName, string displayName, params InputDescriptor[] inputs) => new()
    {
        TypeName = typeName,
        Name = typeName.Split('.').Last(),
        DisplayName = displayName,
        Category = "Test",
        Version = 1,
        IsBrowsable = true,
        Inputs = inputs
    };

    public static InputDescriptor Input(string name, string typeName, bool isWrapped) => new()
    {
        Name = name,
        TypeName = typeName,
        IsWrapped = isWrapped,
        UIHint = "single-line",
        IsBrowsable = true
    };

    private static JsonObject Load(params string[] path) => (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine([AppContext.BaseDirectory, .. path])))!;
}
