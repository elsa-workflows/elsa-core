namespace Elsa.ModularPersistence.Documents;

/// <summary>
/// Describes the optimistic concurrency expectation for a write operation.
/// </summary>
public readonly record struct ExpectedDocumentVersion
{
    private ExpectedDocumentVersion(ExpectedDocumentVersionKind kind, long? version)
    {
        Kind = kind;
        Version = version;
    }

    public ExpectedDocumentVersionKind Kind { get; }

    public long? Version { get; }

    public static ExpectedDocumentVersion Any { get; } = new(ExpectedDocumentVersionKind.Any, null);

    public static ExpectedDocumentVersion New { get; } = new(ExpectedDocumentVersionKind.New, null);

    public static ExpectedDocumentVersion Exact(long version)
    {
        if (version < 0)
            throw new ArgumentOutOfRangeException(nameof(version), "Expected version cannot be negative.");

        return new ExpectedDocumentVersion(ExpectedDocumentVersionKind.Exact, version);
    }
}
