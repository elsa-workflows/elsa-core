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

        var remaining = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var item in Collect(kept))
            remaining[item.Key] = remaining.GetValueOrDefault(item.Key) + 1;

        var dropped = new List<BpmnForeignContentModel>();

        foreach (var item in Collect(stored))
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

    private static bool IsForeign(XNamespace ns) =>
        ns != XNamespace.None
        && !BpmnXmlNames.IsOwnedNamespace(ns)
        && ns != SchemaInstance
        && ns != ElsaBinding
        && ns != LibraryVendor;

    private static IEnumerable<(string Key, BpmnForeignContentModel Model)> Collect(XDocument document)
    {
        var pending = new Stack<(XElement Element, string? OwnerId)>();
        pending.Push((document.Root!, null));

        // Depth-first in document order: children are pushed in reverse so the first one is visited first.
        while (pending.Count > 0)
        {
            var (element, inheritedOwnerId) = pending.Pop();
            var ownerId = (string?)element.Attribute("id") ?? inheritedOwnerId;

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || !IsForeign(attribute.Name.Namespace))
                    continue;

                yield return Item(ownerId, "attribute", attribute.Name, $"={attribute.Value}");
            }

            var children = new List<XElement>();

            foreach (var child in element.Elements())
            {
                if (IsForeign(child.Name.Namespace))
                    yield return Item(ownerId, "element", child.Name, Canonical(child));
                else
                    children.Add(child);
            }

            for (var index = children.Count - 1; index >= 0; index--)
                pending.Push((children[index], ownerId));
        }
    }

    private static (string Key, BpmnForeignContentModel Model) Item(string? ownerId, string kind, XName name, string value)
    {
        var key = new StringBuilder();
        AppendToken(key, ownerId ?? "");
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
        Append(element, builder);
        return builder.ToString();
    }

    private static void Append(XElement element, StringBuilder builder)
    {
        builder.Append('<');
        AppendToken(builder, element.Name.NamespaceName);
        AppendToken(builder, element.Name.LocalName);

        foreach (var attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration).OrderBy(attribute => attribute.Name.ToString(), StringComparer.Ordinal))
        {
            builder.Append('@');
            AppendToken(builder, attribute.Name.NamespaceName);
            AppendToken(builder, attribute.Name.LocalName);
            AppendToken(builder, attribute.Value);
        }

        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XElement child:
                    Append(child, builder);
                    break;
                case XText text when !string.IsNullOrWhiteSpace(text.Value):
                    builder.Append('#');
                    AppendToken(builder, text.Value);
                    break;
            }
        }

        builder.Append('>');
    }

    /// <summary>
    /// Writes <paramref name="value"/> prefixed with its length, so no name, value or text can be read as a boundary:
    /// otherwise an attribute value holding <c>" b="2</c> compares equal to a second attribute.
    /// </summary>
    private static void AppendToken(StringBuilder builder, string value) => builder.Append(value.Length).Append(':').Append(value);
}
