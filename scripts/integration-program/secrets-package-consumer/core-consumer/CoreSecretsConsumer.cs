using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

public static class CoreSecretsConsumer
{
    public static SecretsElsaDbContext Open(string connectionString, IServiceProvider services)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SecretsElsaDbContext>();
        optionsBuilder.UseElsaSqlite(Assembly.Load("Elsa.Secrets.Persistence.EFCore.Sqlite"), connectionString);
        return new SecretsElsaDbContext(optionsBuilder.Options, services);
    }
}
