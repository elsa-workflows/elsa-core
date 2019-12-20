using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class DbContextCustomSchema : IDbContextCustomSchema
    {
        internal static string DefaultMigrationsHistoryTableName { get; } = "__EFMigrationsHistory";

        public DbContextCustomSchema()
        {
            UseCustomSchema = false;
        }
        public DbContextCustomSchema(string schema, string migrationHistoryTableName = "__EFMigrationsHistory")
        {
            Schema = schema;
            MigrationsHistoryTableName = migrationHistoryTableName;
        }

        public bool UseCustomSchema { get; internal set; } = false;
        private string schema = null;
        public string Schema
        {
            get { return schema; }
            set
            {
                if (value != schema)
                {
                    schema = value;
                    UseCustomSchema = !string.IsNullOrWhiteSpace(schema);
                }
            }
        }
        public string MigrationsHistoryTableName { get; set; } = DefaultMigrationsHistoryTableName;
    }
}