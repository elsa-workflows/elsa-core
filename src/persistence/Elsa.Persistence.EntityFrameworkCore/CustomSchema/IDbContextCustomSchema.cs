namespace Elsa.Persistence.EntityFrameworkCore.CustomSchema
{
    public interface IDbContextCustomSchema
    {
        bool UseCustomSchema { get; }
        string Schema { get; set; }
        string MigrationsHistoryTableName { get; set; }
    }
}