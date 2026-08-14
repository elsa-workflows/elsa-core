using System.Text.Json;
using System.Text.Json.Nodes;
using Bpmn.Model;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Bpmn.Interchange.Binding;

/// <summary>
/// The <c>elsa:</c> vendor extension that records which Elsa activity performs a BPMN task the document describes but
/// does not implement, and the one place the names making up that format are defined.
/// </summary>
/// <remarks>
/// <para>
/// <b>Where it lives.</b> Inside the BPMN document, as a vendor extension on the element it binds — not in a side
/// envelope. An exported <c>.bpmn</c> is therefore self-contained and re-importable by itself. <c>Bpmn.Interchange</c>
/// retains any extension element it does not own as typed foreign content and writes it back where it came from, which
/// is what makes this survive a read-modify-write cycle; the library never interprets it.
/// </para>
/// <para>
/// <b>The format.</b> Namespace URI <c>https://elsaworkflows.io/schemas/bpmn/v1</c>, conventional prefix <c>elsa</c>.
/// One <c>&lt;elsa:activityBinding&gt;</c> element inside the BPMN element's <c>&lt;bpmn:extensionElements&gt;</c>:
/// </para>
/// <list type="table">
///   <item>
///     <term><c>activityType</c> — attribute, required</term>
///     <description>
///       The Elsa activity type name as the activity registry keys it, e.g. <c>Elsa.WriteLine</c>. This is
///       <see cref="IActivity.Type"/>, not a CLR type name.
///     </description>
///   </item>
///   <item>
///     <term><c>&lt;elsa:input name="…"&gt;</c> — child element, zero or more, <c>name</c> unique within the binding</term>
///     <description>
///       One per configured activity input. <c>name</c> is the input's property name as it appears in the activity's
///       own JSON (camelCase). The element's text is that single input serialized by Elsa's configured activity
///       serializer, which is the identical <c>{"typeName":…,"expression":…}</c> shape a stored workflow definition
///       uses — so every expression type Elsa knows about round-trips unchanged, and no second encoding of activity
///       inputs has to be kept in step with Elsa's own. A second <c>&lt;elsa:input&gt;</c> naming an input already
///       declared is refused rather than silently taking the later one, the same way an unregistered activity type or
///       a call activity with no <c>calledElement</c> is refused elsewhere in this binder.
///     </description>
///   </item>
/// </list>
/// <para>
/// <b>Escaping.</b> An input's text is JSON, carried as ordinary XML element text — not wrapped in
/// <c>&lt;![CDATA[…]]&gt;</c>. <c>&lt;</c>, <c>&gt;</c> and <c>&amp;</c> inside the JSON (for example, inside a string
/// literal) are therefore XML-escaped as <c>&amp;lt;</c>, <c>&amp;gt;</c> and <c>&amp;amp;</c> the way any XML text
/// node escapes them; <c>Bpmn.Interchange</c> reads and writes this content through <c>System.Xml.Linq</c>, whose
/// standard text-node escaping decodes it back to the original JSON automatically. Nothing else needs to escape or
/// unescape this text: <see cref="Write"/> hands the writer plain JSON, and <see cref="Read"/> reads
/// <see cref="BpmnExtensionElement.Value"/> already decoded.
/// </para>
/// <para>Example:</para>
/// <code>
/// &lt;bpmn:serviceTask id="notify"&gt;
///   &lt;bpmn:extensionElements&gt;
///     &lt;elsa:activityBinding activityType="Elsa.WriteLine"&gt;
///       &lt;elsa:input name="text"&gt;{"typeName":"String","expression":{"type":"JavaScript","value":"getMessage()"}}&lt;/elsa:input&gt;
///     &lt;/elsa:activityBinding&gt;
///   &lt;/bpmn:extensionElements&gt;
/// &lt;/bpmn:serviceTask&gt;
/// </code>
/// <para>
/// <b>Position is the key.</b> The element it sits inside is the element it binds; nothing records a binding ref.
/// That is deliberate: a binding ref is derived by the reader from the element id and a configurable prefix
/// (<c>BpmnImportOptions.BindingRefPrefix</c>), so writing one into the document would make an exported file depend on
/// the import setting that happened to be in force when it was produced.
/// </para>
/// <para>
/// <b>This is a compatibility surface.</b> Changing <see cref="NamespaceUri"/>, <see cref="BindingElementName"/>,
/// <see cref="ActivityTypeAttributeName"/>, <see cref="InputElementName"/> or <see cref="InputNameAttributeName"/>
/// breaks every previously exported <c>.bpmn</c> file: the old element stops being recognised and silently becomes
/// unrelated foreign content, which reads back as a task nobody bound. Anything that reads or writes this shape —
/// Elsa Studio included — has to agree with these constants, so a change here is a versioning decision, not a rename.
/// </para>
/// <para>
/// <b>Disclosure.</b> An exported <c>.bpmn</c> carries the binding configuration verbatim, which includes input
/// expressions: literal values, JavaScript, C#, Liquid, connection or endpoint names — whatever the author put on the
/// activity. Exporting a process is therefore a disclosure of its implementation detail, and a <c>.bpmn</c> from a
/// production tenant should be handled with the same care as the workflow definition it was built from. Nothing is
/// redacted, on purpose: a quietly redacted export produces a file that still looks executable and is not, and the
/// failure only shows up as wrong behaviour after someone re-imports it.
/// </para>
/// </remarks>
public sealed class BpmnActivityBindingFormat(IActivitySerializer activitySerializer)
{
    /// <summary>The namespace URI of the <c>elsa:</c> BPMN vendor extension. See the remarks on this type before changing it.</summary>
    public const string NamespaceUri = "https://elsaworkflows.io/schemas/bpmn/v1";

    /// <summary>The conventional prefix for <see cref="NamespaceUri"/>. Cosmetic in XML, but used verbatim in diagnostics.</summary>
    public const string NamespacePrefix = "elsa";

    /// <summary>The local name of the binding element.</summary>
    public const string BindingElementName = "activityBinding";

    /// <summary>The local name of the attribute naming the Elsa activity type.</summary>
    public const string ActivityTypeAttributeName = "activityType";

    /// <summary>The local name of a single-input child element.</summary>
    public const string InputElementName = "input";

    /// <summary>The local name of the attribute naming the input an <see cref="InputElementName"/> element configures.</summary>
    public const string InputNameAttributeName = "name";

    private static readonly BpmnQName BindingQName = new(NamespaceUri, BindingElementName);
    private static readonly BpmnQName InputQName = new(NamespaceUri, InputElementName);

    /// <summary>
    /// The activity binding declared on the given retained content, or <c>null</c> when it declares none.
    /// </summary>
    public static BpmnExtensionElement? Find(BpmnExtensions? extensions) =>
        extensions?.ExtensionElements.FirstOrDefault(element => element.Name == BindingQName);

    /// <summary>
    /// The given retained content with <paramref name="binding"/> as its activity binding, replacing any it already
    /// carried and leaving every other retained element in place.
    /// </summary>
    /// <remarks>
    /// Adding rather than replacing would leave two <c>activityBinding</c> elements on one BPMN element, and a reader
    /// taking the first of them would silently apply the older one.
    /// </remarks>
    public static BpmnExtensions Attach(BpmnExtensions? extensions, BpmnExtensionElement binding)
    {
        extensions ??= BpmnExtensions.Empty;

        return extensions with
        {
            ExtensionElements = extensions.ExtensionElements.Where(element => element.Name != BindingQName).Append(binding).ToList()
        };
    }

    /// <summary>
    /// The binding element declaring that <paramref name="activity"/> performs the work, with each of its configured
    /// inputs serialized.
    /// </summary>
    public BpmnExtensionElement Write(IActivity activity)
    {
        // Ordered by name so that exporting the same activity twice produces the same bytes, which is what makes a
        // .bpmn file diffable and a round-trip test meaningful.
        var inputs = activity.GetType().GetProperties()
            .Where(property => typeof(Input).IsAssignableFrom(property.PropertyType))
            .Select(property => (Name: JsonNamingPolicy.CamelCase.ConvertName(property.Name), Value: property.GetValue(activity) as Input))
            .Where(input => input.Value is not null)
            .OrderBy(input => input.Name, StringComparer.Ordinal)
            .Select(input => new BpmnExtensionElement(InputQName, [Attribute(InputNameAttributeName, input.Name)], null, activitySerializer.Serialize(input.Value!)))
            .ToList();

        return new(BindingQName, [Attribute(ActivityTypeAttributeName, activity.Type)], inputs);
    }

    /// <summary>
    /// The activity a binding element declares, built through Elsa's own activity serializer so that it is
    /// indistinguishable from the same activity loaded out of a stored workflow definition.
    /// </summary>
    /// <exception cref="BpmnBindingException">The element is malformed, or names an activity type nothing registered.</exception>
    public IActivity Read(BpmnExtensionElement element)
    {
        var activityType = AttributeOf(element, ActivityTypeAttributeName)
                           ?? throw new BpmnBindingException($"An <{NamespacePrefix}:{BindingElementName}> element declares no '{ActivityTypeAttributeName}', so there is nothing to build.");

        var activityJson = new JsonObject
        {
            ["type"] = activityType
        };

        // Every name seen so far, so a second <elsa:input> with the same name is refused rather than silently
        // overwriting activityJson[name] and leaving the earlier one's configuration invisible.
        var seenInputNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var input in element.Children.Where(child => child.Name == InputQName))
        {
            var name = AttributeOf(input, InputNameAttributeName)
                       ?? throw new BpmnBindingException($"An <{NamespacePrefix}:{InputElementName}> element of the '{activityType}' binding declares no '{InputNameAttributeName}'.");

            if (!seenInputNames.Add(name))
                throw new BpmnBindingException($"The '{activityType}' binding declares the input '{name}' more than once. Each <{NamespacePrefix}:{InputElementName}> must name a distinct input.");

            activityJson[name] = Parse(input.Value, name, activityType);
        }

        IActivity activity;

        try
        {
            activity = activitySerializer.Deserialize(activityJson.ToJsonString());
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new BpmnBindingException($"The binding to activity type '{activityType}' could not be deserialized: {exception.Message}");
        }

        // Elsa's activity serializer answers an unregistered type with a NotFoundActivity rather than throwing, and
        // that placeholder only fails once it executes — by which time the workflow has already started and the
        // process is mid-flight. Refusing at bind time turns "this .bpmn needs a module you have not installed" into a
        // sentence naming the type, at the point where someone can still do something about it.
        if (activity is NotFoundActivity)
            throw new BpmnBindingException($"The binding names activity type '{activityType}', which is not registered in this application. Install or enable the module providing it before importing this document.");

        return activity;
    }

    private static JsonNode? Parse(string? json, string inputName, string activityType)
    {
        try
        {
            return JsonNode.Parse(json ?? "null");
        }
        catch (JsonException exception)
        {
            throw new BpmnBindingException($"Input '{inputName}' of the '{activityType}' binding does not hold valid JSON: {exception.Message}");
        }
    }

    private static BpmnForeignAttribute Attribute(string name, string value) => new(new(null, name), value);

    // An unprefixed XML attribute belongs to no namespace, which is what the reader records for these; comparing on
    // the local name alone would also match a same-named attribute some other vendor put in its own namespace.
    private static string? AttributeOf(BpmnExtensionElement element, string name) =>
        element.Attributes.FirstOrDefault(attribute => string.IsNullOrEmpty(attribute.Name.Namespace) && attribute.Name.LocalName == name)?.Value;
}
