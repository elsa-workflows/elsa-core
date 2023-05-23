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

    public string LogFragment => $"Using schema '{_options.SchemaName}'";

    public DbContextOptionsExtensionInfo Info => new CustomDbContextOptionsExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        // Register any required services for your custom database provider here
        // For example:
        // services.AddSingleton<IMyCustomService>(new MyCustomService(_customOption));
    }

    public void Validate(IDbContextOptions options)
    {
        // Perform any validation of the options here
        // For example, check if the custom option is valid
        //if (string.IsNullOrEmpty(_options.SchemaName))
        //{
        //    throw new InvalidOperationException("The custom option must be provided.");
        //}
    }
}
