using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EntityFramework.Core;

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

#if NET6_0_OR_GREATER
        public override int GetServiceProviderHashCode()
        /// <inheritdoc />
    {
        // Return a unique hash code for your custom extension
        return 0;
    }
#else
    public override long GetServiceProviderHashCode()
        /// <inheritdoc />
    {
        // Return a unique hash code for your custom extension
        return 0;
    }
#endif

    /// <inheritdoc />
    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    {
    }

#if NET6_0_OR_GREATER

    /// <inheritdoc />
    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
    {
        return true;
    }
#endif
}
