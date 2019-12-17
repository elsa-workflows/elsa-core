using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class CustomSchemaOptionsExtension : IDbContextOptionsExtension
    {
        public DbContextOptionsExtensionInfo Info => new CustomSchemaExtensionInfo(this);
        
        public IDbContextCustomSchema ContextCustomSchema { get; protected set; }
        public CustomSchemaOptionsExtension(IDbContextCustomSchema customSchema) : base()
        {
            ContextCustomSchema = customSchema;
        }
        protected CustomSchemaOptionsExtension(CustomSchemaOptionsExtension copyFrom)
        {
            copyFrom.ContextCustomSchema = ContextCustomSchema;
        }

        public void ApplyServices(IServiceCollection services)
        {
            
        }

        public void Validate(IDbContextOptions options)
        {
        }

        protected CustomSchemaOptionsExtension Clone()
        {
            return new CustomSchemaOptionsExtension(this);
        }
    }

    public class CustomSchemaExtensionInfo : DbContextOptionsExtensionInfo
    {
        string logFragment = null;
        public CustomSchemaExtensionInfo(IDbContextOptionsExtension dbContextOptionsExtension) : base(dbContextOptionsExtension)
        {
        }

        /// <summary>
        ///     The extension for which this instance contains metadata.
        /// </summary>
        public new virtual CustomSchemaOptionsExtension Extension
            => (CustomSchemaOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(LogFragment)) return logFragment;

                if(Extension.ContextCustomSchema != null && Extension.ContextCustomSchema.UseCustomSchema)
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

        public override long GetServiceProviderHashCode()
        {
            return 0;
        }

        public override void PopulateDebugInfo([NotNullAttribute] IDictionary<string, string> debugInfo)
        {
            debugInfo["CustomSchemaExtensionInfo"] = true.ToString();
        }
    }
}