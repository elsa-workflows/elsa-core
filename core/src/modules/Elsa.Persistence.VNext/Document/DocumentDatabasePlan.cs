namespace Elsa.Persistence.VNext.Document;

public record DocumentDatabasePlan(
    IReadOnlyList<DocumentCollection> Collections);
