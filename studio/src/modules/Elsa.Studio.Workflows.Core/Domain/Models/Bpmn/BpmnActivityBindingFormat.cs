using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// Studio's side of the <c>elsa:</c> BPMN vendor extension that records which Elsa activity performs a task the
/// document describes but does not implement: reading and writing the <c>&lt;elsa:activityBinding&gt;</c> element in
/// the library-format JSON document the <c>bpmn/definitions/{definitionId}/document</c> endpoints exchange.
/// </summary>
/// <remarks>
/// <para>
/// <b>A compatibility surface, owned by elsa-core.</b> Every name here mirrors elsa-core's
/// <c>Elsa.Bpmn.Interchange.Binding.BpmnActivityBindingFormat</c>, and every rule <see cref="Read"/> applies mirrors
/// that type's own <c>Read</c>. Changing any of them breaks every previously exported <c>.bpmn</c> file: the old
/// element stops being recognised and reads back as a task nobody bound.
/// </para>
/// <para>
/// <b>The JSON shape.</b> The document carries a BPMN element's retained vendor content as
/// <c>extensions.extensionElements</c>: <c>Bpmn.Model</c>'s <c>BpmnExtensionElement</c>, a qualified <c>name</c>
/// (<c>ns</c>, <c>localName</c>), an optional text <c>value</c>, <c>attributes</c> and <c>children</c>. The binding is
/// one such element in <see cref="NamespaceUri"/>, with an unqualified <see cref="ActivityTypeAttributeName"/> attribute
/// and one <see cref="InputElementName"/> child per configured input, whose unqualified
/// <see cref="InputNameAttributeName"/> attribute names the input and whose text is that input's JSON.
/// </para>
/// <para>
/// <b>No second encoder.</b> An input's text is exactly the JSON Studio's own input editors wrote into the activity
/// (the same JSON an ordinary workflow-definition save sends to the server's activity serializer), so its shape is
/// whatever that input declares: the <c>{"typeName":…,"expression":…}</c> wrapper for an <c>Input&lt;T&gt;</c>, the
/// value's own JSON for a plain <c>[Input]</c> such as <c>Switch.Cases</c>. Nothing here interprets it.
/// </para>
/// </remarks>
public static class BpmnActivityBindingFormat
{
    /// <summary>The namespace URI of the <c>elsa:</c> BPMN vendor extension.</summary>
    public const string NamespaceUri = "https://elsaworkflows.io/schemas/bpmn/v1";

    /// <summary>The local name of the binding element.</summary>
    public const string BindingElementName = "activityBinding";

    /// <summary>The local name of the attribute naming the Elsa activity type, e.g. <c>Elsa.WriteLine</c>.</summary>
    public const string ActivityTypeAttributeName = "activityType";

    /// <summary>The local name of a single-input child element.</summary>
    public const string InputElementName = "input";

    /// <summary>The local name of the attribute naming the input an <see cref="InputElementName"/> element configures.</summary>
    public const string InputNameAttributeName = "name";

    /// <summary>
    /// Keeps an input's JSON text readable in the exported <c>.bpmn</c>: an expression such as <c>a &lt; b</c> is
    /// written as-is rather than as <c>\u003C</c>. The XML writer escapes the text node itself, so relaxing JSON's
    /// HTML-safe escaping changes nothing a reader sees once the text is parsed.
    /// </summary>
    private static readonly JsonSerializerOptions InputTextOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// The activity binding declared on <paramref name="element"/> — a <c>BpmnElement</c> of the document — or
    /// <see langword="null"/> when it declares none. Like elsa-core's own <c>Find</c>, the first one wins.
    /// </summary>
    public static JsonObject? Find(JsonObject element) =>
        GetExtensionElements(element)?.OfType<JsonObject>().FirstOrDefault(IsBinding);

    /// <summary>
    /// Builds the binding element declaring that an activity of <paramref name="activityType"/> performs the work, with
    /// one input element per entry of <paramref name="inputs"/> whose value is not JSON <c>null</c>, ordered by name
    /// so that writing the same configuration twice produces the same document — the rules elsa-core's own
    /// <c>Write</c> applies.
    /// </summary>
    /// <param name="activityType">The activity registry's type name, e.g. <c>Elsa.WriteLine</c>; never a CLR name.</param>
    /// <param name="inputs">Each input's camelCase name and its JSON, as the activity's own input editors wrote it.</param>
    public static JsonObject Create(string activityType, IEnumerable<KeyValuePair<string, JsonNode?>> inputs)
    {
        var inputElements = inputs
            .Where(input => input.Value is not null && input.Value.GetValueKind() != JsonValueKind.Null)
            .OrderBy(input => input.Key, StringComparer.Ordinal)
            .Select(input => (JsonNode)CreateElement(InputElementName, Attribute(InputNameAttributeName, input.Key), input.Value!.ToJsonString(InputTextOptions), []));

        return CreateElement(BindingElementName, Attribute(ActivityTypeAttributeName, activityType), null, inputElements);
    }

    /// <summary>
    /// Reads <paramref name="binding"/> with elsa-core's read rules: an <see cref="ActivityTypeAttributeName"/> is
    /// required, every input names itself, no input is named twice, and every input's text is JSON.
    /// </summary>
    /// <exception cref="BpmnActivityBindingFormatException">The binding breaks one of those rules.</exception>
    public static BpmnActivityBinding Read(JsonObject binding)
    {
        var activityType = AttributeOf(binding, ActivityTypeAttributeName)
                           ?? throw new BpmnActivityBindingFormatException($"The <elsa:{BindingElementName}> element declares no '{ActivityTypeAttributeName}'.");

        var inputs = new List<BpmnActivityBindingInput>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var input in (binding["children"] as JsonArray ?? []).OfType<JsonObject>().Where(child => HasName(child, InputElementName)))
        {
            var name = AttributeOf(input, InputNameAttributeName)
                       ?? throw new BpmnActivityBindingFormatException($"An <elsa:{InputElementName}> element of the '{activityType}' binding declares no '{InputNameAttributeName}'.");

            if (!names.Add(name))
                throw new BpmnActivityBindingFormatException($"The '{activityType}' binding declares the input '{name}' more than once.");

            inputs.Add(new(name, ParseInput(input["value"]?.GetValue<string>(), name, activityType)));
        }

        return new(activityType, inputs);
    }

    /// <summary>
    /// Makes <paramref name="binding"/> the activity binding of <paramref name="element"/>, leaving every other
    /// retained extension element, documentation entry, foreign attribute and foreign child exactly where it was. An
    /// existing binding is replaced in place; any further ones are removed, since a reader taking the first of two
    /// would silently apply the older one.
    /// </summary>
    public static void Attach(JsonObject element, JsonObject binding)
    {
        var extensionElements = GetOrCreateExtensionElements(element);
        var existing = extensionElements.Select((node, index) => (Node: node, Index: index)).Where(entry => entry.Node is JsonObject candidate && IsBinding(candidate)).Select(entry => entry.Index).ToList();

        if (existing.Count == 0)
        {
            extensionElements.Add(binding);
            return;
        }

        for (var i = existing.Count - 1; i > 0; i--)
            extensionElements.RemoveAt(existing[i]);

        extensionElements[existing[0]] = binding;
    }

    private static bool IsBinding(JsonObject extensionElement) => HasName(extensionElement, BindingElementName);

    private static bool HasName(JsonObject extensionElement, string localName) =>
        extensionElement["name"] is JsonObject name
        && name["ns"]?.GetValue<string>() == NamespaceUri
        && name["localName"]?.GetValue<string>() == localName;

    // An unprefixed XML attribute belongs to no namespace; comparing on the local name alone would also match a
    // same-named attribute another vendor put in its own namespace.
    private static string? AttributeOf(JsonObject extensionElement, string localName) =>
        (extensionElement["attributes"] as JsonArray ?? [])
        .OfType<JsonObject>()
        .FirstOrDefault(attribute => attribute["name"] is JsonObject name
                                     && string.IsNullOrEmpty(name["ns"]?.GetValue<string>())
                                     && name["localName"]?.GetValue<string>() == localName)?["value"]?.GetValue<string>();

    private static JsonNode? ParseInput(string? json, string name, string activityType)
    {
        try
        {
            return JsonNode.Parse(json ?? "null");
        }
        catch (JsonException exception)
        {
            throw new BpmnActivityBindingFormatException($"Input '{name}' of the '{activityType}' binding does not hold valid JSON: {exception.Message}");
        }
    }

    private static JsonArray? GetExtensionElements(JsonObject element) => element["extensions"]?["extensionElements"] as JsonArray;

    private static JsonArray GetOrCreateExtensionElements(JsonObject element)
    {
        if (element["extensions"] is not JsonObject extensions)
            element["extensions"] = extensions = new() { ["documentation"] = new JsonArray(), ["foreignAttributes"] = new JsonArray(), ["foreignChildren"] = new JsonArray() };

        if (extensions["extensionElements"] is not JsonArray extensionElements)
            extensions["extensionElements"] = extensionElements = [];

        return extensionElements;
    }

    private static JsonObject CreateElement(string localName, JsonObject attribute, string? value, IEnumerable<JsonNode> children) => new()
    {
        ["name"] = QualifiedName(NamespaceUri, localName),
        ["value"] = value,
        ["attributes"] = new JsonArray(attribute),
        ["children"] = new JsonArray(children.ToArray())
    };

    private static JsonObject Attribute(string localName, string value) => new()
    {
        ["name"] = QualifiedName(null, localName),
        ["value"] = value
    };

    private static JsonObject QualifiedName(string? ns, string localName) => new()
    {
        ["ns"] = ns,
        ["localName"] = localName
    };
}

/// <summary>An <c>elsa:activityBinding</c> as <see cref="BpmnActivityBindingFormat.Read"/> reads it.</summary>
/// <param name="ActivityType">The activity registry's type name the binding names.</param>
/// <param name="Inputs">Each configured input, in document order.</param>
public sealed record BpmnActivityBinding(string ActivityType, IReadOnlyList<BpmnActivityBindingInput> Inputs);

/// <summary>One configured input of a <see cref="BpmnActivityBinding"/>.</summary>
/// <param name="Name">The input's camelCase property name, as the activity declares it.</param>
/// <param name="Value">The input's JSON, parsed.</param>
public sealed record BpmnActivityBindingInput(string Name, JsonNode? Value);

/// <summary>
/// An <c>elsa:activityBinding</c> breaks one of the rules elsa-core refuses it for at import; see
/// <see cref="BpmnActivityBindingFormat.Read"/>.
/// </summary>
public sealed class BpmnActivityBindingFormatException(string message) : Exception(message);
