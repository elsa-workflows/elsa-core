using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Describes a bounded document query expressed against declared indexes.
/// </summary>
public sealed record DocumentQuery
{
    public DocumentQuery(
        string documentType,
        IEnumerable<DocumentQueryFilter>? filters = null,
        IEnumerable<DocumentQuerySort>? sorts = null,
        DocumentQueryPage? page = null)
    {
        DocumentType = DescriptorValidation.RequireName(documentType, nameof(documentType));
        Filters = (filters ?? []).ToArray();
        Sorts = (sorts ?? []).ToArray();
        Page = page;
    }

    public string DocumentType { get; }

    public IReadOnlyList<DocumentQueryFilter> Filters { get; }

    public IReadOnlyList<DocumentQuerySort> Sorts { get; }

    public DocumentQueryPage? Page { get; }
}
