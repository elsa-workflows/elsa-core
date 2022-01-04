namespace Elsa.Persistence.EntityFrameworkCore.Contracts;

/// <summary>
/// An optional handler to implement when entities require specific handling of serialization / deserialization when being written to or loaded from the database.
/// </summary>
public interface IEntitySerializer<in TEntity>
{
    void Serialize(ElsaDbContext dbContext, TEntity entity);
    void Deserialize(ElsaDbContext dbContext, TEntity entity);
}