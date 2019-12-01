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
        public DbContextCustomSchema(string customSchema, string customMigrationHistoryTableName = "__EFMigrationsHistory")
        {
            CustomDefaultSchema = customSchema;
            CustomMigrationsHistoryTableName = customMigrationHistoryTableName;
        }

        public bool UseCustomSchema { get; internal set; } = false;
        private string _customDefaultSchema = null;
        public string CustomDefaultSchema
        {
            get { return _customDefaultSchema; }
            set
            {
                if (value != _customDefaultSchema)
                {
                    _customDefaultSchema = value;
                    UseCustomSchema = !string.IsNullOrWhiteSpace(_customDefaultSchema);
                }
            }
        }
        public string CustomMigrationsHistoryTableName { get; set; } = DefaultMigrationsHistoryTableName;
    }
}