using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Humanizer;

namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The activity the BPMN "Performed by" section lets a user configure for one task: the activity JSON Studio's own
/// input editors read and write — the very editors the flowchart's activity properties panel uses — and the
/// <c>elsa:activityBinding</c> that JSON becomes.
/// </summary>
/// <remarks>
/// <para>
/// <b>The invariant.</b> Reading a binding into a draft and writing the draft back reconstructs every input the binding
/// carried, value for value: an input's JSON moves between the binding and the activity as a JSON node, never through
/// an encoder of Studio's own, so what the editors wrote is exactly what the server's activity serializer reads, the
/// same as for an ordinary workflow-definition save.
/// </para>
/// <para>
/// <b>Two spellings of one name.</b> The binding names an input the way the server does,
/// <see cref="JsonNamingPolicy.CamelCase"/> applied to the property name; the input editors key the activity JSON with
/// Humanizer's <c>Camelize</c>. The two agree for ordinary names and disagree for an acronym (<c>URL</c> is
/// <c>url</c> to one and <c>uRL</c> to the other), so each input is mapped through its descriptor rather than by
/// assuming either spelling.
/// </para>
/// <para>
/// <b>Nothing is dropped quietly.</b> A binding input no descriptor input accounts for — possible only when Studio's
/// activity catalogue is older than the server's — is written back exactly as it was read, so saving a draft never
/// loses configuration Studio merely cannot display; the server remains the one that accepts or refuses it.
/// </para>
/// </remarks>
public sealed class BpmnActivityBindingDraft
{
    private readonly IReadOnlyList<BpmnActivityBindingInput> _unmappedInputs;

    private BpmnActivityBindingDraft(ActivityDescriptor descriptor, JsonObject activity, IReadOnlyList<BpmnActivityBindingInput> unmappedInputs)
    {
        Descriptor = descriptor;
        Activity = activity;
        _unmappedInputs = unmappedInputs;
    }

    /// <summary>The descriptor of the activity type the task is bound to.</summary>
    public ActivityDescriptor Descriptor { get; }

    /// <summary>
    /// The activity JSON the input editors edit in place. Never part of any workflow definition's own activity graph,
    /// so editing it cannot change the graph an ordinary save would send.
    /// </summary>
    public JsonObject Activity { get; }

    /// <summary>
    /// A draft for a freshly picked activity type, carrying the descriptor's construction properties the way a
    /// flowchart drop does.
    /// </summary>
    /// <param name="descriptor">The picked activity type.</param>
    /// <param name="activityId">The id the editors key the activity on; any stable id for the task.</param>
    public static BpmnActivityBindingDraft Create(ActivityDescriptor descriptor, string activityId)
    {
        var activity = CreateActivity(descriptor, activityId);

        foreach (var property in descriptor.ConstructionProperties)
            activity[property.Key.Camelize()] = JsonSerializer.SerializeToNode(property.Value);

        return new(descriptor, activity, []);
    }

    /// <summary>A draft carrying every input <paramref name="binding"/> configures.</summary>
    /// <param name="binding">The binding the document declares.</param>
    /// <param name="descriptor">The descriptor of <see cref="BpmnActivityBinding.ActivityType"/>.</param>
    /// <param name="activityId">The id the editors key the activity on; any stable id for the task.</param>
    public static BpmnActivityBindingDraft FromBinding(BpmnActivityBinding binding, ActivityDescriptor descriptor, string activityId)
    {
        var activity = CreateActivity(descriptor, activityId);
        var inputsByName = binding.Inputs.ToDictionary(input => input.Name, StringComparer.Ordinal);

        foreach (var input in descriptor.Inputs)
        {
            if (inputsByName.Remove(BindingNameOf(input), out var bound))
                activity[EditorNameOf(input)] = bound.Value?.DeepClone();
        }

        return new(descriptor, activity, binding.Inputs.Where(input => inputsByName.ContainsKey(input.Name)).ToList());
    }

    /// <summary>
    /// The <c>elsa:activityBinding</c> element for the draft as it stands: one input per descriptor input the editors
    /// have given a value, plus any input the draft carried through unmapped.
    /// </summary>
    public JsonObject ToBinding() => BpmnActivityBindingFormat.Create(Descriptor.TypeName, Inputs());

    private IEnumerable<KeyValuePair<string, JsonNode?>> Inputs() =>
        Descriptor.Inputs
            .Select(input => KeyValuePair.Create(BindingNameOf(input), Activity[EditorNameOf(input)]))
            .Concat(_unmappedInputs.Select(input => KeyValuePair.Create(input.Name, input.Value)));

    private static JsonObject CreateActivity(ActivityDescriptor descriptor, string activityId) => new()
    {
        ["id"] = activityId,
        ["type"] = descriptor.TypeName,
        ["version"] = descriptor.Version
    };

    /// <summary>How elsa-core names the input in a binding: <c>JsonNamingPolicy.CamelCase</c> of the property name.</summary>
    private static string BindingNameOf(InputDescriptor input) => JsonNamingPolicy.CamelCase.ConvertName(input.Name);

    /// <summary>How Studio's input editors key the input in the activity JSON (see <c>InputsTab</c>).</summary>
    private static string EditorNameOf(InputDescriptor input) => input.Name.Camelize();
}
