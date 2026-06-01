namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Describes the portable filter operations available to declared-index queries.
/// </summary>
public enum DocumentQueryFilterOperator
{
    Equals = 0,
    NotEquals = 1,
    In = 2,
    GreaterThan = 3,
    GreaterThanOrEqual = 4,
    LessThan = 5,
    LessThanOrEqual = 6,
    Between = 7,
    StartsWith = 8,
    IsNull = 9,
    IsNotNull = 10
}
