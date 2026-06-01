namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Describes portable query behavior supported by a document provider.
/// </summary>
public sealed record DocumentQueryCapabilities
{
    public DocumentQueryCapabilities(IEnumerable<DocumentQueryFilterOperator> supportedOperators)
    {
        ArgumentNullException.ThrowIfNull(supportedOperators);

        var operators = supportedOperators.ToArray();
        if (operators.Length == 0)
            throw new ArgumentException("At least one query operator must be supported.", nameof(supportedOperators));

        foreach (var queryOperator in operators)
        {
            if (!Enum.IsDefined(queryOperator))
                throw new ArgumentOutOfRangeException(nameof(supportedOperators), queryOperator, "Unknown query operator.");
        }

        SupportedOperators = operators.Distinct().ToArray();
    }

    public IReadOnlyCollection<DocumentQueryFilterOperator> SupportedOperators { get; }

    public static DocumentQueryCapabilities Portable { get; } = new(
        [
            DocumentQueryFilterOperator.Equals,
            DocumentQueryFilterOperator.NotEquals,
            DocumentQueryFilterOperator.In,
            DocumentQueryFilterOperator.GreaterThan,
            DocumentQueryFilterOperator.GreaterThanOrEqual,
            DocumentQueryFilterOperator.LessThan,
            DocumentQueryFilterOperator.LessThanOrEqual,
            DocumentQueryFilterOperator.Between,
            DocumentQueryFilterOperator.IsNull,
            DocumentQueryFilterOperator.IsNotNull
        ]);

    public static DocumentQueryCapabilities WithStartsWith { get; } = new(Portable.SupportedOperators.Append(DocumentQueryFilterOperator.StartsWith));
}
