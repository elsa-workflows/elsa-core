using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Multitenant;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Multitenancy.Extensions;
using Elsa.Options;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.EntityFramework.Core.StartupTasks;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    public static class ElsaOptionsBuilderExtensions
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
        /// <param name="autoRunMigrations">If <c>true</c> then database migrations will be auto-executed on startup</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseEntityFrameworkPersistence(this ElsaOptionsBuilder elsa,
            Action<DbContextOptionsBuilder> configure,
            bool autoRunMigrations = true) =>
            elsa.UseEntityFrameworkPersistence<ElsaContext>(configure, autoRunMigrations);

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
        /// <param name="autoRunMigrations">If <c>true</c> then database migrations will be auto-executed on startup</param>
        /// <typeparam name="TElsaContext">The concrete type of <see cref="ElsaContext"/> to use.</typeparam>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseEntityFrameworkPersistence<TElsaContext>(this ElsaOptionsBuilder elsa,
            Action<DbContextOptionsBuilder> configure,
            bool autoRunMigrations = true) where TElsaContext : ElsaContext =>
            elsa.UseEntityFrameworkPersistence<TElsaContext>((_, builder) => configure(builder), autoRunMigrations);

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
        /// <param name="autoRunMigrations">If <c>true</c> then database migrations will be auto-executed on startup</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseEntityFrameworkPersistence(this ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            bool autoRunMigrations = true) =>
            elsa.UseEntityFrameworkPersistence<ElsaContext>(configure, autoRunMigrations);

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
        /// <param name="autoRunMigrations">If <c>true</c> then database migrations will be auto-executed on startup</param>
        /// <typeparam name="TElsaContext">The concrete type of <see cref="ElsaContext"/> to use.</typeparam>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseEntityFrameworkPersistence<TElsaContext>(this ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            bool autoRunMigrations = true) where TElsaContext : ElsaContext =>
            UseEntityFrameworkPersistence<TElsaContext>(elsa, configure, autoRunMigrations, true, ServiceLifetime.Singleton);

        /// <summary>
        /// Configures Elsa to use Entity Framework Core for persistence, without using pooled DB Context instances.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method when you do not wish to use DB connection pooling, such as when integrating with a multi-tenant
        /// application, where re-use of DB Context objects is impractical.
        /// </para>
        /// <para>
        /// Although auto-running of migrations is supported in this scenario, use this with caution. When pooling is not in use and each instance of
        /// the DB Context may differ, it is not feasible to try to automatically migrate them.
        /// Your application is ultimately responsible for executing the contents of the <see cref="RunMigrations"/> class in a manner
        /// which is suitable for your use-case.
        /// </para>
        /// </remarks>
        /// <param name="elsa">An Elsa options builder</param>
        /// <param name="configure">A configuration builder callback</param>
        /// <param name="serviceLifetime">The service lifetime which will be used for each DB Context instance</param>
        /// <param name="autoRunMigrations">If <c>true</c> then database migrations will be auto-executed on startup</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseNonPooledEntityFrameworkPersistence(this ElsaOptionsBuilder elsa,
            Action<DbContextOptionsBuilder> configure,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton,
            bool autoRunMigrations = false) =>
            elsa.UseNonPooledEntityFrameworkPersistence<ElsaContext>(configure, serviceLifetime, autoRunMigrations);

        /// <summary>
        /// Configures Elsa to use Entity Framework Core for persistence, without using pooled DB Context instances.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method when you do not wish to use DB connection pooling, such as when integrating with a multi-tenant
        /// application, where re-use of DB Context objects is impractical.
        /// </para>
        /// <para>
        /// Although auto-running of migrations is supported in this scenario, use this with caution. When pooling is not in use and each instance of
        /// the DB Context may differ, it is not feasible to try to automatically migrate them.
        /// Your application is ultimately responsible for executing the contents of the <see cref="RunMigrations"/> class in a manner
        /// which is suitable for your use-case.
        /// </para>
        /// </remarks>
        /// <param name="elsa">An Elsa options builder</param>
        /// <param name="configure">A configuration builder callback</param>
        /// <param name="serviceLifetime">The service lifetime which will be used for each DB Context instance</param>
        /// <param name="autoRunMigrations">If <c>true</c> then database migrations will be auto-executed on startup</param>
        /// <typeparam name="TElsaContext">The concrete type of <see cref="ElsaContext"/> to use.</typeparam>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseNonPooledEntityFrameworkPersistence<TElsaContext>(this ElsaOptionsBuilder elsa,
            Action<DbContextOptionsBuilder> configure,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton,
            bool autoRunMigrations = false) where TElsaContext : ElsaContext =>
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
        /// Although auto-running of migrations is supported in this scenario, use this with caution. When pooling is not in use and each instance of
        /// the DB Context may differ, it is not feasible to try to automatically migrate them.
        /// Your application is ultimately responsible for executing the contents of the <see cref="RunMigrations"/> class in a manner
        /// which is suitable for your use-case.
        /// </para>
        /// </remarks>
        /// <param name="elsa">An Elsa options builder</param>
        /// <param name="configure">A configuration builder callback, which also provides access to a service provider</param>
        /// <param name="serviceLifetime">The service lifetime which will be used for each DB Context instance</param>
        /// <param name="autoRunMigrations">If <c>true</c> then database migrations will be auto-executed on startup</param>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseNonPooledEntityFrameworkPersistence(this ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton,
            bool autoRunMigrations = false) =>
            elsa.UseNonPooledEntityFrameworkPersistence<ElsaContext>(configure, serviceLifetime, autoRunMigrations);

        /// <summary>
        /// Configures Elsa to use Entity Framework Core for persistence, without using pooled DB Context instances.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method when you do not wish to use DB connection pooling, such as when integrating with a multi-tenant
        /// application, where re-use of DB Context objects is impractical.
        /// </para>
        /// <para>
        /// Although auto-running of migrations is supported in this scenario, use this with caution. When pooling is not in use and each instance of
        /// the DB Context may differ, it is not feasible to try to automatically migrate them.
        /// Your application is ultimately responsible for executing the contents of the <see cref="RunMigrations"/> class in a manner
        /// which is suitable for your use-case.
        /// </para>
        /// </remarks>
        /// <param name="elsa">An Elsa options builder</param>
        /// <param name="configure">A configuration builder callback, which also provides access to a service provider</param>
        /// <param name="serviceLifetime">The service lifetime which will be used for each DB Context instance</param>
        /// <param name="autoRunMigrations">If <c>true</c> then database migrations will be auto-executed on startup</param>
        /// <typeparam name="TElsaContext">The concrete type of <see cref="ElsaContext"/> to use.</typeparam>
        /// <returns>The Elsa options builder, so calls may be chained</returns>
        public static ElsaOptionsBuilder UseNonPooledEntityFrameworkPersistence<TElsaContext>(this ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton,
            bool autoRunMigrations = false) where TElsaContext : ElsaContext =>
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
                .AddScoped<EntityFrameworkBookmarkStore>()
                .AddScoped<EntityFrameworkTriggerStore>();

            if (autoRunMigrations)
                elsa.ContainerBuilder.AddStartupTask<RunMigrations>();

            return elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowExecutionLogRecordStore>())
                .UseBookmarkStore(sp => sp.GetRequiredService<EntityFrameworkBookmarkStore>())
                .UseTriggerStore(sp => sp.GetRequiredService<EntityFrameworkTriggerStore>());
        }

        public static ElsaOptionsBuilder UseEntityFrameworkPersistenceForMultitenancy(this ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            bool autoRunMigrations) => UseEntityFrameworkPersistenceForMultitenancy<ElsaContext>(elsa, configure, autoRunMigrations);
        private static ElsaOptionsBuilder UseEntityFrameworkPersistenceForMultitenancy<TElsaContext>(ElsaOptionsBuilder elsa,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            bool autoRunMigrations) where TElsaContext : ElsaContext
        {
            //tODO: find a way to register DbContextFactory with Autofac
            elsa.Services.AddDbContextFactory<TElsaContext>(configure, ServiceLifetime.Scoped);

            elsa.ContainerBuilder
                .AddMultiton<IElsaContextFactory, ElsaContextFactory<TElsaContext>>()
                .AddScoped<EntityFrameworkWorkflowDefinitionStore>()
                .AddScoped<EntityFrameworkWorkflowInstanceStore>()
                .AddScoped<EntityFrameworkWorkflowExecutionLogRecordStore>()
                .AddScoped<EntityFrameworkBookmarkStore>()
                .AddScoped<EntityFrameworkTriggerStore>();

            elsa.ContainerBuilder
                .Register(cc =>
                {
                    var tenant = cc.Resolve<ITenant>();
                    return new ElsaDbOptions(tenant!.GetDatabaseConnectionString()!);
                }).InstancePerTenant();

            if (autoRunMigrations)
                elsa.ContainerBuilder.AddStartupTask<RunMigrations>();

            return elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowExecutionLogRecordStore>())
                .UseBookmarkStore(sp => sp.GetRequiredService<EntityFrameworkBookmarkStore>())
                .UseTriggerStore(sp => sp.GetRequiredService<EntityFrameworkTriggerStore>());
        }

        // tODO: This code mimics AddDbContextFactory extension method for IServiceCollection
        //       and allows to register DbContextFactory as multiton.
        //       However, it touches internal API's which is not good.
        //       Need to find a better approach.
        //private static void AddDbContextFactory<TContext>(this ContainerBuilder containerBuilder, Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction) where TContext : DbContext
        //{
        //    containerBuilder.Register(cc =>
        //    {
        //        var sp = cc.Resolve<IServiceProvider>();
        //        return CreateDbContextOptions<TContext>(sp, optionsAction);
        //    }).As<DbContextOptions<TContext>>().IfNotRegistered(typeof(DbContextOptions<TContext>)).InstancePerTenant();

        //    containerBuilder.Register(cc => cc.Resolve<DbContextOptions<TContext>>()).As<DbContextOptions>().InstancePerTenant();
        //    containerBuilder.RegisterType<DbContextFactorySource<TContext>>().As<IDbContextFactorySource<TContext>>().InstancePerTenant();

        //    containerBuilder.RegisterType<DbContextFactory<TContext>>().As<IDbContextFactory<TContext>>().IfNotRegistered(typeof(IDbContextFactory<TContext>)).InstancePerTenant();

        //    containerBuilder.RegisterType<TContext>().As<TContext>().IfNotRegistered(typeof(TContext)).InstancePerLifetimeScope();
        //}

        //private static DbContextOptions<TContext> CreateDbContextOptions<TContext>(
        //    IServiceProvider applicationServiceProvider,
        //    Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction) where TContext : DbContext
        //{
        //    var builder = new DbContextOptionsBuilder<TContext>(
        //        new DbContextOptions<TContext>(new Dictionary<Type, IDbContextOptionsExtension>()));

        //    builder.UseApplicationServiceProvider(applicationServiceProvider);

        //    optionsAction?.Invoke(applicationServiceProvider, builder);

        //    return builder.Options;
        //}
    }
}
