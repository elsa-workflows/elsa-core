using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using Elsa.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.EntityFrameworkCore.Common.Abstractions;

/// <summary>
/// A design-time factory supporting various database providers.
/// </summary>
public abstract class DesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext> where TDbContext : DbContext
{
    /// <summary>
    /// Gets the assembly containing the migrations.
    /// </summary>
    protected abstract Assembly Assembly { get; }
    
    /// <inheritdoc />
    public TDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        var providerOption = new Option<string>("--provider", "Specifies the provider.");
        var connectionStringOption = new Option<string>("--connectionString", "Specifies the connection string.");
        var command = new RootCommand();

        command.AddOption(providerOption);
        command.AddOption(connectionStringOption);

        var parser = new Parser(command);
        var parseResult = parser.Parse(args);
        var provider = parseResult.GetValueForOption(providerOption) ?? throw new ArgumentException("Provider not specified.");
        var connectionString = parseResult.GetValueForOption(connectionStringOption) ?? "Data Source=local";
        var assembly = GetType().Assembly;

        switch (provider.ToLowerInvariant())
        {
            case "sqlite":
                builder.UseElsaSqlite(assembly, connectionString);
                break;
            case "sqlserver":
                builder.UseElsaSqlServer(assembly, connectionString);
                break;
            case "mysql":
                builder.UseElsaMySql(assembly, connectionString);
                break;
            case "postgresql":
                builder.UseElsaPostgreSql(assembly, connectionString);
                break;
            default:
                throw new ArgumentException($"Unknown provider: {provider}");
        }

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), builder.Options)!;
    }
}