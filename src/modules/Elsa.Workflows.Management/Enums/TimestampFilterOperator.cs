namespace Elsa.Workflows.Management.Enums;

/// <summary>
/// Specifies the operators for filtering timestamps in a TimestampFilter object.
/// </summary>
public enum TimestampFilterOperator
{
    /// <summary>
    /// The timestamp is equal to the specified value.
    /// </summary>
    Is,

    /// <summary>
    /// The timestamp is not equal to the specified value.
    /// </summary>
    IsNot,

    /// <summary>
    /// The timestamp is before the specified value.
    /// </summary>
    LessThan,

    /// <summary>
    /// The timestamp is greater than the specified value.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// The timestamp is less than or equal to the specified value.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// The timestamp is greater than or equal to the specified value.
    /// </summary>
    GreaterThanOrEqual
}