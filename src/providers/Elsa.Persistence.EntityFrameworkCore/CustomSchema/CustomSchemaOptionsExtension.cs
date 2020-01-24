using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.CustomSchema
{
    public class CustomSchemaOptionsExtension : IDbContextOptionsExtension
    {
        public DbContextOptionsExtensionInfo Info => new CustomSchemaOptionsExtensionInfo(this);

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
    }
}