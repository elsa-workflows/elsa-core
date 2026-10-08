namespace Elsa.Persistence.VNext.MongoDb;

public record MongoDbDatabasePlan(
    IReadOnlyList<MongoDbCollectionPlan> Collections);
