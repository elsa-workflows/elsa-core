using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EntityFrameworkCore.CustomSchema
{
    public class CustomSchemaModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public CustomSchemaModelCacheKeyFactory()
        {
        }

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