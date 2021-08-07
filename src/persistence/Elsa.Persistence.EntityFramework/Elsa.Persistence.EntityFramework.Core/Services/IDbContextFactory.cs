namespace Elsa.Persistence.EntityFramework.Core.Services
{
    public interface IContextFactory<out TContext>
    {
        TContext CreateDbContext();
    }
}