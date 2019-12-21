using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Persistence.EntityFrameworkCore.CustomSchema
{
    public static class CustomSchemaDbContextBuilderExtensions
    {
        public static EntityFrameworkCoreElsaBuilder AddCustomSchema(this ElsaBuilder configuration, string schema, string migrationHistoryTableName = null)
        {
            EntityFrameworkCoreElsaBuilder frameworkCoreElsaBuilder = new EntityFrameworkCoreElsaBuilder(configuration.Services);
            if (string.IsNullOrWhiteSpace(schema))
            {
                return frameworkCoreElsaBuilder;
            }
            DbContextCustomSchema dbContextCustomSchema = new DbContextCustomSchema(schema,
                    !string.IsNullOrWhiteSpace(migrationHistoryTableName) ? migrationHistoryTableName : DbContextCustomSchema.DefaultMigrationsHistoryTableName);

            configuration.Services.AddSingleton<IDbContextCustomSchema>(dbContextCustomSchema);

            return frameworkCoreElsaBuilder;
        }

        public static SqliteDbContextOptionsBuilder AddCustomSchemaModelSupport(this SqliteDbContextOptionsBuilder sqliteDbContextOptionsBuilder, DbContextOptionsBuilder dbContextOptionsBuilder, IServiceCollection services)
        {
            services.AddTransient<CustomSchemaOptionsExtension>();
            dbContextOptionsBuilder.ReplaceService<IModelCacheKeyFactory, CustomSchemaModelCacheKeyFactory>();
            sqliteDbContextOptionsBuilder.AddCustomSchemaExtension(services);

            return sqliteDbContextOptionsBuilder;
        }

        public static SqlServerDbContextOptionsBuilder AddCustomSchemaModelSupport(this SqlServerDbContextOptionsBuilder sqlServerDbContextOptionsBuilder, DbContextOptionsBuilder dbContextOptionsBuilder, IServiceCollection services)
        {
            services.AddTransient<CustomSchemaOptionsExtension>();
            dbContextOptionsBuilder.ReplaceService<IModelCacheKeyFactory, CustomSchemaModelCacheKeyFactory>();
            sqlServerDbContextOptionsBuilder.AddCustomSchemaExtension(services);

            return sqlServerDbContextOptionsBuilder;
        }

        public static NpgsqlDbContextOptionsBuilder AddCustomSchemaModelSupport(this NpgsqlDbContextOptionsBuilder npgsqlDbContextOptionsBuilder, DbContextOptionsBuilder dbContextOptionsBuilder, IServiceCollection services)
        {
            services.AddTransient<CustomSchemaOptionsExtension>();
            dbContextOptionsBuilder.ReplaceService<IModelCacheKeyFactory, CustomSchemaModelCacheKeyFactory>();
            npgsqlDbContextOptionsBuilder.AddCustomSchemaExtension(services);

            return npgsqlDbContextOptionsBuilder;
        }

        public static SqliteDbContextOptionsBuilder AddCustomSchemaExtension(this SqliteDbContextOptionsBuilder sqliteDbContextOptionsBuilder, IServiceCollection services)
        {
            var infrastructure = sqliteDbContextOptionsBuilder as IRelationalDbContextOptionsBuilderInfrastructure;
            infrastructure.AddCustomSchemaExtension(services);
            return sqliteDbContextOptionsBuilder;
        }

        public static SqlServerDbContextOptionsBuilder AddCustomSchemaExtension(this SqlServerDbContextOptionsBuilder sqlServerDbContextOptionsBuilder, IServiceCollection services)
        {
            var infrastructure = sqlServerDbContextOptionsBuilder as IRelationalDbContextOptionsBuilderInfrastructure;
            infrastructure.AddCustomSchemaExtension(services);
            return sqlServerDbContextOptionsBuilder;
        }

        public static NpgsqlDbContextOptionsBuilder AddCustomSchemaExtension(this NpgsqlDbContextOptionsBuilder npgsqlDbContextOptionsBuilder, IServiceCollection services)
        {
            var infrastructure = npgsqlDbContextOptionsBuilder as IRelationalDbContextOptionsBuilderInfrastructure;
            infrastructure.AddCustomSchemaExtension(services);
            return npgsqlDbContextOptionsBuilder;            
        }

        static void AddCustomSchemaExtension(this IRelationalDbContextOptionsBuilderInfrastructure relationalDbContextOptionsBuilderInfrastructure, IServiceCollection services)
        {
            if (relationalDbContextOptionsBuilderInfrastructure != null)
            {
                var builder = relationalDbContextOptionsBuilderInfrastructure.OptionsBuilder as IDbContextOptionsBuilderInfrastructure;
                if (builder != null)
                {
                    // if the extension is registered already then we keep it 
                    // otherwise we create a new one
                    var extension = relationalDbContextOptionsBuilderInfrastructure.OptionsBuilder.Options.FindExtension<CustomSchemaOptionsExtension>();
                    if (extension == null)
                    {
                        using (var scope = services.BuildServiceProvider().CreateScope())
                        {
                            builder.AddOrUpdateExtension(scope.ServiceProvider.GetService<CustomSchemaOptionsExtension>());
                        }
                    }
                }
            }
        }

        public static SqliteDbContextOptionsBuilder MigrationsHistoryTableWithSchema(this SqliteDbContextOptionsBuilder sqliteDbContextOptionsBuilder, DbContextOptionsBuilder optionsBuilder)
        {
            if (sqliteDbContextOptionsBuilder != null)
            {
                IDbContextCustomSchema dbContextCustomSchema = optionsBuilder.GetDbContextCustomSchema();

                if (dbContextCustomSchema != null && dbContextCustomSchema.UseCustomSchema)
                {
                    sqliteDbContextOptionsBuilder.MigrationsHistoryTable(dbContextCustomSchema.MigrationsHistoryTableName, dbContextCustomSchema.Schema);
                }
            }
            return sqliteDbContextOptionsBuilder;
        }

        public static SqlServerDbContextOptionsBuilder MigrationsHistoryTableWithSchema(this SqlServerDbContextOptionsBuilder sqlServerDbContextOptionsBuilder, DbContextOptionsBuilder optionsBuilder)
        {
            if (sqlServerDbContextOptionsBuilder != null)
            {
                IDbContextCustomSchema dbContextCustomSchema = optionsBuilder.GetDbContextCustomSchema();

                if (dbContextCustomSchema != null && dbContextCustomSchema.UseCustomSchema)
                {
                    sqlServerDbContextOptionsBuilder.MigrationsHistoryTable(dbContextCustomSchema.MigrationsHistoryTableName, dbContextCustomSchema.Schema);
                }
            }
            return sqlServerDbContextOptionsBuilder;
        }

        public static NpgsqlDbContextOptionsBuilder MigrationsHistoryTableWithSchema(this NpgsqlDbContextOptionsBuilder npgsqlDbContextOptionsBuilder, DbContextOptionsBuilder optionsBuilder)
        {
            if (npgsqlDbContextOptionsBuilder != null)
            {
                IDbContextCustomSchema dbContextCustomSchema = optionsBuilder.GetDbContextCustomSchema();

                if (dbContextCustomSchema != null && dbContextCustomSchema.UseCustomSchema)
                {
                    npgsqlDbContextOptionsBuilder.MigrationsHistoryTable(dbContextCustomSchema.MigrationsHistoryTableName, dbContextCustomSchema.Schema);
                }
            }
            return npgsqlDbContextOptionsBuilder;
        }

        public static IDbContextCustomSchema GetDbContextCustomSchema(this DbContextOptionsBuilder optionsBuilder)
        {
            IDbContextCustomSchema dbContextCustomSchema = null;
            if (optionsBuilder != null)
            {
                return optionsBuilder.Options.GetDbContextCustomSchema();
            }
            return dbContextCustomSchema;
        }

        public static IDbContextCustomSchema GetDbContextCustomSchema(this DbContextOptions<ElsaContext> options)
        {
            return GetDbContextCustomSchema((DbContextOptions)options);
        }

        public static IDbContextCustomSchema GetDbContextCustomSchema(this DbContextOptions options)
        {
            IDbContextCustomSchema dbContextCustomSchema = null;
            if (options != null)
            {
                var extension = options.FindExtension<CustomSchemaOptionsExtension>();
                if (extension != null)
                {
                    dbContextCustomSchema = extension.ContextCustomSchema;
                }
            }
            return dbContextCustomSchema;
        }
    }
}