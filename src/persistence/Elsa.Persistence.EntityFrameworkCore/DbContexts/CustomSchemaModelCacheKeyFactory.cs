using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class CustomSchemaModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public CustomSchemaModelCacheKeyFactory() { }
        public object Create(DbContext context)
        {
            string schema = null;
            if (context is ElsaContext)
            {
                var dbContextCustomSchema = ((ElsaContext)context).DbContextCustomSchema;
                if (dbContextCustomSchema != null && dbContextCustomSchema.UseCustomSchema)
                {
                    schema = dbContextCustomSchema.Schema;
                }
            }
            return new
            {
                Type = context.GetType(),
                Schema = schema
            };
        }
    }
}