using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.Oracle;

[UsedImplicitly]
public class AIDbContextFactory : OracleAIDesignTimeDbContextFactory<AIDbContext>;

public class OracleAIDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        var options = new ElsaDbContextOptions().ConfigureOracle();
        builder.UseElsaOracle(GetType().Assembly, connectionString, options);
    }
}
