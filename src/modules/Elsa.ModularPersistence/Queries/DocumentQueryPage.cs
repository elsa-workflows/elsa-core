namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Describes bounded paging for document queries.
/// </summary>
public sealed record DocumentQueryPage
{
    public DocumentQueryPage(int limit, int offset = 0)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "Query page limit must be greater than zero.");

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Query page offset cannot be negative.");

        Limit = limit;
        Offset = offset;
    }

    public int Limit { get; }

    public int Offset { get; }
}
