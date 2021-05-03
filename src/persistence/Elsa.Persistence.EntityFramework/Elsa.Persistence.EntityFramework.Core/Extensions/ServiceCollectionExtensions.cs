using System;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.EntityFramework.Core.StartupTasks;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseEntityFrameworkPersistence(this ElsaOptionsBuilder elsa,
            Action<DbContextOptionsBuilder> configure) =>
            elsa.UseEntityFrameworkPersistence<ElsaContext>(configure);

        public static ElsaOptionsBuilder UseEntityFrameworkPersistence<TElsaContext>(this ElsaOptionsBuilder elsa,
            Action<DbContextOptionsBuilder> configure) where TElsaContext : ElsaContext =>
            elsa.UseEntityFrameworkPersistence<TElsaContext>((_, builder) => configure(builder));

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
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseEntityFrameworkPersistence(this ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure) =>
            elsa.UseEntityFrameworkPersistence<ElsaContext>(configure);

        public static ElsaOptionsBuilder UseEntityFrameworkPersistence<TElsaContext>(this ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure) where TElsaContext : ElsaContext =>
            UseEntityFrameworkPersistence<TElsaContext>(elsa, configure, false, true, ServiceLifetime.Singleton);

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
        /// <param name="autoRunMigrations">If <c>true</c> then migration scripts will auto-run on application startup; if <c>false</c> then they will not</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseNonPooledEntityFrameworkPersistence(this ElsaOptionsBuilder elsa,
            Action<DbContextOptionsBuilder> configure,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton,
            bool autoRunMigrations = true) =>
            elsa.UseNonPooledEntityFrameworkPersistence<ElsaContext>(configure, serviceLifetime, autoRunMigrations);

        public static ElsaOptionsBuilder UseNonPooledEntityFrameworkPersistence<TElsaContext>(this ElsaOptionsBuilder elsa,
            Action<DbContextOptionsBuilder> configure,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton,
            bool autoRunMigrations = true) where TElsaContext : ElsaContext =>
            elsa.UseNonPooledEntityFrameworkPersistence<TElsaContext>((_, builder) => configure(builder), serviceLifetime, autoRunMigrations);

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
        /// /// <param name="autoRunMigrations">If <c>true</c> then migration scripts will auto-run on application startup; if <c>false</c> then they will not</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseNonPooledEntityFrameworkPersistence(this ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton,
            bool autoRunMigrations = true) =>
            elsa.UseNonPooledEntityFrameworkPersistence<ElsaContext>(configure, serviceLifetime, autoRunMigrations);

        public static ElsaOptionsBuilder UseNonPooledEntityFrameworkPersistence<TElsaContext>(this ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton,
            bool autoRunMigrations = true) where TElsaContext : ElsaContext =>
            UseEntityFrameworkPersistence<TElsaContext>(elsa, configure, autoRunMigrations, false, serviceLifetime);

        static ElsaOptionsBuilder UseEntityFrameworkPersistence<TElsaContext>(ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            bool autoRunMigrations,
            bool useContextPooling,
            ServiceLifetime serviceLifetime) where TElsaContext : ElsaContext
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

            if (useContextPooling)
                elsa.Services.AddPooledDbContextFactory<TElsaContext>(configure);
            else
                elsa.Services.AddDbContextFactory<TElsaContext>(configure, serviceLifetime);

            elsa.Services
                .AddSingleton<IElsaContextFactory, ElsaContextFactory<TElsaContext>>()
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