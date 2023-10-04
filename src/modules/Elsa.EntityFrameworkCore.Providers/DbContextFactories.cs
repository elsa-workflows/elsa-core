using System.Reflection;
using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Providers;

/// <summary>
/// A design-time factory for Identity supporting various database providers.
/// </summary>
public class IdentityDbContextFactory : DesignTimeDbContextFactory<IdentityElsaDbContext>
{
}

/// <summary>
/// A design-time factory for Management supporting various database providers.
/// </summary>
public class ManagementDbContextFactory : DesignTimeDbContextFactory<ManagementElsaDbContext>
{
}

/// <summary>
/// A design-time factory for Runtime supporting various database providers.
/// </summary>
public class RuntimeDbContextFactory : DesignTimeDbContextFactory<RuntimeElsaDbContext>
{
}

/// <summary>
/// A design-time factory for Runtime supporting various database providers.
/// </summary>
public class LabelsDbContextFactory : DesignTimeDbContextFactory<LabelsElsaDbContext>
{
}

/// <summary>
/// A design-time factory for Alterations supporting various database providers.
/// </summary>
public class AlterationsDbContextFactories : DesignTimeDbContextFactory<AlterationsElsaDbContext>
{
}

/// <inheritdoc />
public class DesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    /// <inheritdoc />
    protected override Assembly Assembly => GetType().Assembly;
}