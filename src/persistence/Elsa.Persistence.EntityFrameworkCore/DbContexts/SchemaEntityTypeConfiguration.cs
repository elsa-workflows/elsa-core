using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
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