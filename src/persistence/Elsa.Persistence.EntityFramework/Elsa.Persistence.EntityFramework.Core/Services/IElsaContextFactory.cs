namespace Elsa.Persistence.EntityFramework.Core.Services
{
    public interface IElsaContextFactory
    {
        ElsaContext CreateDbContext();
    }
}