namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Represents a query that failed planning before execution.
/// </summary>
public class DocumentQueryException(DocumentQueryPlan plan, string message) : Exception(message)
{
    public DocumentQueryPlan Plan { get; } = plan;
}
