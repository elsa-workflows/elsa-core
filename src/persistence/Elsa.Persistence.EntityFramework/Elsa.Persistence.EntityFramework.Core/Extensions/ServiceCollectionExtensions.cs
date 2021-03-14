using System;
using Elsa.Persistence.EntityFramework.Core.StartupTasks;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures Elsa to use Entity Framework Core for persistence, using pooled DB Context instances.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Pooled DB Context instances is a performance optimisation which is documented in more detail at
        /// https://docs.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-constant#dbcontext-pooling.
        /// </para>
        /// </remarks>
        /// <param name="elsa">An Elsa options builder</param>
        /// <param name="configure">A configuration builder callback</param>
        /// <param name="autoRunMigrations">If <c>true</c> then migration scripts will auto-run on application startup; if <c>false</c> then they will not</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptions UseEntityFrameworkPersistence(this ElsaOptions elsa,
                                                                Action<DbContextOptionsBuilder> configure,
                                                                bool autoRunMigrations = true) =>
            elsa.UseEntityFrameworkPersistence((_, builder) => configure(builder), autoRunMigrations);

        /// <summary>
        /// Configures Elsa to use Entity Framework Core for persistence, using pooled DB Context instances.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Pooled DB Context instances is a performance optimisation which is documented in more detail at
        /// https://docs.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-constant#dbcontext-pooling.
        /// </para>
        /// </remarks>
        /// <param name="elsa">An Elsa options builder</param>
        /// <param name="configure">A configuration builder callback, which also provides access to a service provider</param>
        /// <param name="autoRunMigrations">If <c>true</c> then migration scripts will auto-run on application startup; if <c>false</c> then they will not</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptions UseEntityFrameworkPersistence(this ElsaOptions elsa,
                                                                Action<IServiceProvider, DbContextOptionsBuilder> configure,
                                                                bool autoRunMigrations = true) =>
            UseEntityFrameworkPersistence(elsa, configure, autoRunMigrations, true, ServiceLifetime.Singleton);

        /// <summary>
        /// Configures Elsa to use Entity Framework Core for persistence, without using pooled DB Context instances.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method when you do not wish to use DB connection pooling, such as when integrating with a multi-tenant
        /// application, where re-use of DB Context objects is impractical.
        /// </para>
        /// <para>
        /// Auto-running of migrations is not supported in this scenario.  When pooling is not in use and each instance of
        /// the DB Context may differ, it is not feasible to try to automatically migrate them.
        /// Your application is responsible for executing the contents of the <see cref="RunMigrations"/> class in a manner
        /// which is suitable for your use-case.
        /// </para>
        /// </remarks>
        /// <param name="elsa">An Elsa options builder</param>
        /// <param name="configure">A configuration builder callback</param>
        /// <param name="serviceLifetime">The service lifetime which will be used for each DB Context instance</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptions UseNonPooledEntityFrameworkPersistence(this ElsaOptions elsa,
                                                                         Action<DbContextOptionsBuilder> configure,
                                                                         ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) =>
            elsa.UseNonPooledEntityFrameworkPersistence((_, builder) => configure(builder), serviceLifetime);

        /// <summary>
        /// Configures Elsa to use Entity Framework Core for persistence, without using pooled DB Context instances.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method when you do not wish to use DB connection pooling, such as when integrating with a multi-tenant
        /// application, where re-use of DB Context objects is impractical.
        /// </para>
        /// <para>
        /// Auto-running of migrations is not supported in this scenario.  When pooling is not in use and each instance of
        /// the DB Context may differ, it is not feasible to try to automatically migrate them.
        /// Your application is responsible for executing the contents of the <see cref="RunMigrations"/> class in a manner
        /// which is suitable for your use-case.
        /// </para>
        /// </remarks>
        /// <param name="elsa">An Elsa options builder</param>
        /// <param name="configure">A configuration builder callback, which also provides access to a service provider</param>
        /// <param name="serviceLifetime">The service lifetime which will be used for each DB Context instance</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptions UseNonPooledEntityFrameworkPersistence(this ElsaOptions elsa,
                                                                         Action<IServiceProvider, DbContextOptionsBuilder> configure,
                                                                         ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) =>
            UseEntityFrameworkPersistence(elsa, configure, false, false, serviceLifetime);

        static ElsaOptions UseEntityFrameworkPersistence(ElsaOptions elsa,
                                                         Action<IServiceProvider, DbContextOptionsBuilder> configure,
                                                         bool autoRunMigrations,
                                                         bool useContextPooling,
                                                         ServiceLifetime serviceLifetime)
        {
            /* Auto-running migrations is intentionally unavailable when not using context pooling.
             * When we aren't using pooling then it probably means that each DB Context is different
             * in some manner.  That could easily mean the connection strings (IE: Contexts might not
             * all connect to the same DB).  In that case, without further logic (which can't be
             * pre-empted by Elsa), we can't be sure we're connecting to the right DBs when running
             * migrations.
             *
             * It's much more sane just to explicitly not-support it and leave it to the app developer.
             * They can run their own migrations in line with their own logic.
             */

            if(useContextPooling)
                elsa.Services.AddPooledDbContextFactory<ElsaContext>(configure);
            else
                elsa.Services.AddDbContextFactory<ElsaContext>(configure, serviceLifetime);

            elsa.Services
                .AddScoped<EntityFrameworkWorkflowDefinitionStore>()
                .AddScoped<EntityFrameworkWorkflowInstanceStore>()
                .AddScoped<EntityFrameworkWorkflowExecutionLogRecordStore>()
                .AddScoped<EntityFrameworkBookmarkStore>();

            if (autoRunMigrations)
                elsa.Services.AddStartupTask<RunMigrations>();

            return elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowExecutionLogRecordStore>())
                .UseWorkflowTriggerStore(sp => sp.GetRequiredService<EntityFrameworkBookmarkStore>());
        }
    }
}