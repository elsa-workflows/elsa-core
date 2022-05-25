namespace Elsa.Persistence.EntityFrameworkCore.Common.Services;

/// <summary>
/// An optional handler to implement when entities require specific handling of serialization / deserialization when being written to or loaded from the database.
/// </summary>
public interface IEntitySerializer<in TDbContext, in TEntity>
{
    void Serialize(TDbContext dbContext, TEntity entity);
    void Deserialize(TDbContext dbContext, TEntity entity);
}