using System.Text;
using System.Xml;
using System.Xml.Linq;
using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.Binding;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Xml;

/// <summary>
/// Finds the foreign content a stored BPMN document has that a new one does not: what the XML <c>PUT</c> reports as
/// dropped, so a client that saved without the designer's load-warning acknowledgement still learns what it lost.
/// </summary>
/// <remarks>
/// <para>
/// Foreign content is an element or attribute in a namespace that is neither BPMN's own (model, DI, DC, DD), nor
/// <c>xsi</c> or <c>xml</c>, nor Elsa's binding namespace or the library's vendor namespace. Changes to those are
/// what an edit is: they change the bound graph or the layout. Content in any other namespace means something only to
/// some other tool, and is what a modeler is most likely to lose without anyone noticing.
/// </para>
/// <para>
/// Each piece is identified by the nearest element id that carries it, its namespace and name, and its value: a
/// foreign element by its whole subtree, compared by namespace and local name rather than prefix, with attribute order
/// and whitespace-only text ignored. Content found under a different owner, or with a different value, counts as
/// dropped. Elements are compared as multisets, so losing one of two identical extensions is reported once.
/// </para>
/// </remarks>
internal static class BpmnForeignContent
{
    private static readonly XNamespace SchemaInstance = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace ElsaBinding = BpmnActivityBindingFormat.NamespaceUri;
    private static readonly XNamespace LibraryVendor = BpmnXmlNames.VendorNamespaceName;
    private static readonly XName Process = (XNamespace)BpmnXmlNames.Model + "process";

    // Not a valid XML id, so it cannot collide with an element's own.
    private const string SingleProcessOwnerKey = "\0process";

    /// <summary>
    /// The foreign content in <paramref name="storedXml"/> that <paramref name="newXml"/> no longer has, in document
    /// order. Empty when there is no stored document, or either one cannot be parsed.
    /// </summary>
    public static IReadOnlyList<BpmnForeignContentModel> DroppedBetween(string? storedXml, string newXml)
    {
        var stored = Parse(storedXml);
        var kept = Parse(newXml);

        if (stored is null || kept is null)
            return [];

        // A document with one process binds it whatever its id is now, so when both have one, renaming it does not
        // move the content it carries.
        var storedProcess = SingleProcess(stored);
        var keptProcess = SingleProcess(kept);
        var processesCorrespond = storedProcess is not null && keptProcess is not null;

        var remaining = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var item in Collect(kept, processesCorrespond ? keptProcess : null))
            remaining[item.Key] = remaining.GetValueOrDefault(item.Key) + 1;

        var dropped = new List<BpmnForeignContentModel>();

        foreach (var item in Collect(stored, processesCorrespond ? storedProcess : null))
        {
            if (remaining.TryGetValue(item.Key, out var count) && count > 0)
                remaining[item.Key] = count - 1;
            else
                dropped.Add(item.Model);
        }

        return dropped;
    }

    private static XDocument? Parse(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            // Both documents come from clients; a DTD has no place in BPMN, so none is processed.
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            return XDocument.Load(reader);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static XElement? SingleProcess(XDocument document)
    {
        var processes = document.Root!.Elements(Process).Take(2).ToList();
        return processes.Count == 1 ? processes[0] : null;
    }

    private static bool IsForeign(XNamespace ns) =>
        ns != XNamespace.None
        && !BpmnXmlNames.IsOwnedNamespace(ns)
        && ns != SchemaInstance
        && ns != ElsaBinding
        && ns != LibraryVendor;

    /// <param name="document">The document to collect from.</param>
    /// <param name="singleProcess">
    /// The document's only process when the other document also has only one, so the content each carries is compared
    /// under one owner whatever their ids; otherwise null, and every owner is compared by its id.
    /// </param>
    private static IEnumerable<(string Key, BpmnForeignContentModel Model)> Collect(XDocument document, XElement? singleProcess)
    {
        var pending = new Stack<(XElement Element, string? OwnerId, string? OwnerKey)>();
        pending.Push((document.Root!, null, null));

        // Depth-first in document order: children are pushed in reverse so the first one is visited first.
        while (pending.Count > 0)
        {
            var (element, inheritedOwnerId, inheritedOwnerKey) = pending.Pop();
            var id = (string?)element.Attribute("id");
            var ownerId = id ?? inheritedOwnerId;
            var ownerKey = element == singleProcess ? SingleProcessOwnerKey : id ?? inheritedOwnerKey;

            foreach (var attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration && IsForeign(attribute.Name.Namespace)))
                yield return Item(ownerId, ownerKey, "attribute", attribute.Name, attribute.Value);

            var children = new List<XElement>();

            foreach (var child in element.Elements())
            {
                if (IsForeign(child.Name.Namespace))
                    yield return Item(ownerId, ownerKey, "element", child.Name, Canonical(child));
                else
                    children.Add(child);
            }

            for (var index = children.Count - 1; index >= 0; index--)
                pending.Push((children[index], ownerId, ownerKey));
        }
    }

    private static (string Key, BpmnForeignContentModel Model) Item(string? ownerId, string? ownerKey, string kind, XName name, string value)
    {
        var key = new StringBuilder();
        AppendToken(key, ownerKey ?? "");
        AppendToken(key, kind);
        AppendToken(key, name.NamespaceName);
        AppendToken(key, name.LocalName);
        AppendToken(key, value);

        return (key.ToString(), new BpmnForeignContentModel { ElementId = ownerId, Kind = kind, Namespace = name.NamespaceName, Name = name.LocalName });
    }

    /// <summary>The element's subtree by namespace and local name, so a changed prefix or attribute order is not a difference.</summary>
    private static string Canonical(XElement element)
    {
        var builder = new StringBuilder();

        // An explicit stack rather than recursion, so a deeply nested extension cannot exhaust the call stack. A null
        // entry closes the element whose children were pushed above it.
        var pending = new Stack<object?>();
        pending.Push(element);

        while (pending.Count > 0)
        {
            switch (pending.Pop())
            {
                case null:
                    builder.Append('>');
                    break;
                case XElement current:
                    builder.Append('<');
                    AppendToken(builder, current.Name.NamespaceName);
                    AppendToken(builder, current.Name.LocalName);

                    foreach (var attribute in current.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration).OrderBy(attribute => attribute.Name.ToString(), StringComparer.Ordinal))
                    {
                        builder.Append('@');
                        AppendToken(builder, attribute.Name.NamespaceName);
                        AppendToken(builder, attribute.Name.LocalName);
                        AppendToken(builder, attribute.Value);
                    }

                    pending.Push(null);

                    foreach (var child in ChildrenWithTextJoined(current).Reverse())
                        pending.Push(child);

                    break;
                case string text when !string.IsNullOrWhiteSpace(text):
                    builder.Append('#');
                    AppendToken(builder, text);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// The element's child elements, with each run of text between them joined into one string: text split across
    /// CDATA sections, or around a comment, is the same text as when written in one piece.
    /// </summary>
    private static IEnumerable<object> ChildrenWithTextJoined(XElement element)
    {
        var text = new StringBuilder();

        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText piece:
                    text.Append(piece.Value);
                    break;
                case XElement child:
                    if (text.Length > 0)
                    {
                        yield return text.ToString();
                        text.Clear();
                    }

                    yield return child;
                    break;
            }
        }

        if (text.Length > 0)
            yield return text.ToString();
    }

    /// <summary>
    /// Writes <paramref name="value"/> prefixed with its length, so no name, value or text can be read as a boundary:
    /// otherwise an attribute value holding <c>" b="2</c> compares equal to a second attribute.
    /// </summary>
    private static void AppendToken(StringBuilder builder, string value) => builder.Append(value.Length).Append(':').Append(value);
}
