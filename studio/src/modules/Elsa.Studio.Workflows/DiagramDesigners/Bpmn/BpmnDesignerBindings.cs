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
            {
                throw new BpmnActivityBindingFormatException($"The '{binding.ActivityType}' binding declares the input '{input.Name}' more than once.");
            }

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

    /// <summary>
    /// Whether two bindings name the same activity with the same input values, whatever their order or spacing. An input
    /// whose value is JSON <c>null</c> counts as absent, since <see cref="BpmnActivityBindingFormat.Create"/> writes none.
    /// A binding that names an input twice matches nothing, so the section reads it and reports it as <see cref="Read"/> does.
    /// </summary>
    public static bool AreEquivalent(BpmnDesignerBinding? left, BpmnDesignerBinding? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (HasRepeatedName(left) || HasRepeatedName(right))
        {
            return false;
        }

        var leftInputs = ValuedInputs(left);
        var rightInputs = ValuedInputs(right);

        if (left.ActivityType != right.ActivityType || leftInputs.Count != rightInputs.Count)
        {
            return false;
        }

        var rightByName = rightInputs.ToDictionary(input => input.Name, StringComparer.Ordinal);

        return leftInputs.All(input => rightByName.TryGetValue(input.Name, out var other) && IsSameJson(input.Value, other.Value));
    }

    private static bool HasRepeatedName(BpmnDesignerBinding binding) =>
        binding.Inputs.Select(input => input.Name).Distinct(StringComparer.Ordinal).Count() != binding.Inputs.Count;

    private static List<BpmnDesignerBindingInput> ValuedInputs(BpmnDesignerBinding binding) =>
        binding.Inputs.Where(input => !IsJsonNull(input.Value)).ToList();

    private static bool IsJsonNull(string value)
    {
        try
        {
            return JsonNode.Parse(value) is null;
        }
        catch (JsonException)
        {
            return false;
        }
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
