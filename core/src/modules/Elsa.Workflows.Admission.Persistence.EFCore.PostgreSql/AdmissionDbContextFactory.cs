using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql;

public sealed class AdmissionDbContextFactory : DesignTimeDbContextFactoryBase<AdmissionElsaDbContext>
{
    protected override void ConfigureServices(IServiceCollection services) => services.AddPostgreSqlEntityModelCreatingHandlers();

    protected override void ConfigureBuilder(DbContextOptionsBuilder<AdmissionElsaDbContext> builder, string connectionString) =>
        builder.UseElsaPostgreSql(GetType().Assembly, connectionString, new ElsaDbContextOptions { MigrationsHistoryTableName = "__AdmissionMigrationsHistory" });
}
