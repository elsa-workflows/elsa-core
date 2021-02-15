using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EntityFrameworkCore.CustomSchema
{
    public sealed class CustomSchemaOptionsExtensionInfo : DbContextOptionsExtensionInfo
    {
        private string logFragment;

        public CustomSchemaOptionsExtensionInfo(IDbContextOptionsExtension dbContextOptionsExtension) : base(dbContextOptionsExtension)
        {
        }

        /// <summary>
        /// The extension for which this instance contains metadata.
        /// </summary>
        private new CustomSchemaOptionsExtension Extension => (CustomSchemaOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(logFragment)) 
                    return logFragment;

                if (Extension.ContextCustomSchema != null && Extension.ContextCustomSchema.UseCustomSchema)
                {
                    var builder = new StringBuilder();

                    builder.Append($"Use Custom Schema: {Extension.ContextCustomSchema.UseCustomSchema}");
                    builder.Append($"Custom Schema: {Extension.ContextCustomSchema.Schema}");
                    builder.Append($"Migrations History Table Name: {Extension.ContextCustomSchema.MigrationsHistoryTableName}");

                    logFragment = builder.ToString();
                }

                return logFragment;
            }
        }

        public override long GetServiceProviderHashCode() => 0;

        public override void PopulateDebugInfo([NotNull] IDictionary<string, string> debugInfo)
        {
            debugInfo["CustomSchemaExtensionInfo"] = true.ToString();
        }
    }
}