using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

public static class LegacySecretsConsumer
{
    public static SecretsDbContext Open(string connectionString, IServiceProvider services)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SecretsDbContext>();
        optionsBuilder.UseElsaSqlite(Assembly.Load("Elsa.Secrets.Persistence.EFCore.Sqlite"), connectionString);
        return new SecretsDbContext(optionsBuilder.Options, services);
    }
}
