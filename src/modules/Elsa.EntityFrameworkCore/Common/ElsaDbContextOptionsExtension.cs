using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Common;

public class ElsaDbContextOptionsExtension : IDbContextOptionsExtension
{
    private ElsaDbContextOptions _options;

    public ElsaDbContextOptionsExtension(ElsaDbContextOptions? options)
    {
        _options = options;
    }

    public ElsaDbContextOptions Options => _options;

    public string LogFragment => "";

    public DbContextOptionsExtensionInfo Info => new CustomDbContextOptionsExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
    }

    public void Validate(IDbContextOptions options)
    {
        if(!string.IsNullOrWhiteSpace(_options.SchemaName) && string.IsNullOrWhiteSpace(_options.MigrationsAssemblyName))
        {
            throw new ArgumentException("MigrationsAssemblyName must be defined for manual migration");
        }
    }
}