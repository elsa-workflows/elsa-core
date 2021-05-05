using System;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Testing.Shared.Helpers;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Elsa.Persistence.EntityFramework.Sqlite;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithEntityFrameworkAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            var tempFolder = new TemporaryFolder();

            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.UseEntityFrameworkPersistence(opts => {
                        opts.UseSqlite($"Data Source={tempFolder.GetContainedPath("elsa.db")};", db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name));
                    });
                });
            };
        }
    }
}