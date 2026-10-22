using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// Converts between the binding a BPMN designer's <c>getBinding</c> and <c>setBinding</c> exchange, whose input values
/// are JSON text, and the <see cref="BpmnActivityBinding"/> the "Performed by" drafts are built from.
/// </summary>
internal static class BpmnDesignerBindings
{
    /// <summary>Writes an input's JSON the way <see cref="BpmnActivityBindingFormat.Create"/> does, so the text reads the same in the document.</summary>
    private static readonly JsonSerializerOptions InputTextOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Reads <paramref name="binding"/> with the rules <see cref="BpmnActivityBindingFormat.Read"/> applies: every input
    /// named once, and every value JSON.
    /// </summary>
    /// <exception cref="BpmnActivityBindingFormatException">The binding breaks one of those rules.</exception>
    public static BpmnActivityBinding Read(BpmnDesignerBinding binding)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var inputs = new List<BpmnActivityBindingInput>();

        foreach (var input in binding.Inputs)
        {
            if (!names.Add(input.Name))
                throw new BpmnActivityBindingFormatException($"The '{binding.ActivityType}' binding declares the input '{input.Name}' more than once.");

            inputs.Add(new(input.Name, Parse(input, binding.ActivityType)));
        }

        return new(binding.ActivityType, inputs);
    }

    /// <summary>The binding <paramref name="draft"/> describes, as a designer's <c>setBinding</c> takes it.</summary>
    public static BpmnDesignerBinding From(BpmnActivityBindingDraft draft)
    {
        var binding = BpmnActivityBindingFormat.Read(draft.ToBinding());
        return new(binding.ActivityType, binding.Inputs.Select(input => new BpmnDesignerBindingInput(input.Name, input.Value!.ToJsonString(InputTextOptions))).ToList());
    }

    /// <summary>Whether two bindings name the same activity with the same input values, whatever their order or spacing.</summary>
    public static bool AreEquivalent(BpmnDesignerBinding? left, BpmnDesignerBinding? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        if (left.ActivityType != right.ActivityType || left.Inputs.Count != right.Inputs.Count)
            return false;

        var rightInputs = right.Inputs.ToDictionary(input => input.Name, StringComparer.Ordinal);

        return left.Inputs.All(input => rightInputs.TryGetValue(input.Name, out var other) && IsSameJson(input.Value, other.Value));
    }

    private static JsonNode? Parse(BpmnDesignerBindingInput input, string activityType)
    {
        try
        {
            return JsonNode.Parse(input.Value);
        }
        catch (JsonException exception)
        {
            throw new BpmnActivityBindingFormatException($"The input '{input.Name}' of the '{activityType}' binding is not JSON: {exception.Message}");
        }
    }

    private static bool IsSameJson(string left, string right)
    {
        try
        {
            return JsonNode.DeepEquals(JsonNode.Parse(left), JsonNode.Parse(right));
        }
        catch (JsonException)
        {
            return left == right;
        }
    }
}
