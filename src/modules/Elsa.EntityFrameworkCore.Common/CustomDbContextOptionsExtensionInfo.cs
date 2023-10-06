using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.EntityFrameworkCore.Common;

/// <summary>
/// Contains options for configuring Elsa's Entity Framework Core integration.
/// </summary>
public class CustomDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
{
    /// <inheritdoc />
    public CustomDbContextOptionsExtensionInfo(IDbContextOptionsExtension extension)
        : base(extension)
    {
    }

    /// <inheritdoc />
    public override bool IsDatabaseProvider => false;

    /// <inheritdoc />
    public override string LogFragment => "";

    /// <inheritdoc />
    public override int GetServiceProviderHashCode()
    {
        // Return a unique hash code for your custom extension
        return 0;
    }

    /// <inheritdoc />
    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    {
    }

    /// <inheritdoc />
    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
    {
        return true;
    }
}
