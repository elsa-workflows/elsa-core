namespace Elsa.Persistence.VNext.MongoDb;

public record MongoDbIndexPlan(
    string Name,
    IReadOnlyList<string> Fields,
    bool IsUnique);
