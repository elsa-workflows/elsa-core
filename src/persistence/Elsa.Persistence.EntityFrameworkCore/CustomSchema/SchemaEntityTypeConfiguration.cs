using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.CustomSchema
{
    public class SchemaEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : class
    {
        private readonly IDbContextCustomSchema dbContextCustomSchema;
        public SchemaEntityTypeConfiguration(IDbContextCustomSchema dbContextCustomSchema)
        {
            this.dbContextCustomSchema = dbContextCustomSchema;
        }
        public void Configure(EntityTypeBuilder<TEntity> builder)
        {
            if (dbContextCustomSchema != null && dbContextCustomSchema.UseCustomSchema)
            {
                Type type = typeof(TEntity);
                builder.ToTable(type.Name, dbContextCustomSchema.Schema);
            }
        }
    }
}