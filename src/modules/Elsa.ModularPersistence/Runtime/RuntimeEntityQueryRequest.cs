using Elsa.ModularPersistence.Queries;

namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeEntityQueryRequest
{
    public RuntimeEntityQueryRequest(IEnumerable<RuntimeEntityQueryFilter> filters, IEnumerable<DocumentQuerySort>? sorts = null, int? limit = null, int offset = 0)
    {
        Filters = filters?.ToArray() ?? throw new ArgumentNullException(nameof(filters));
        Sorts = (sorts ?? []).ToArray();
        Limit = limit;
        Offset = offset;
    }

    public IReadOnlyList<RuntimeEntityQueryFilter> Filters { get; }

    public IReadOnlyList<DocumentQuerySort> Sorts { get; }

    public int? Limit { get; }

    public int Offset { get; }
}
