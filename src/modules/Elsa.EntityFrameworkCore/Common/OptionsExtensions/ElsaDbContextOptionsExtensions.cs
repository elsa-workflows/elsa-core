using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.EntityFrameworkCore.Common;

public static class ElsaDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseElsaDbContextOptions(
        this DbContextOptionsBuilder optionsBuilder,
        ElsaDbContextOptions? options)
    {
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(
            new ElsaDbContextOptionsExtension(options));

        return optionsBuilder;
    }
}
