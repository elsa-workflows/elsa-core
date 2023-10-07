using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.EntityFrameworkCore.Common.Abstractions;

/// <summary>
/// A design-time factory base class that can be inherited from by provider-specific implementations.
/// </summary>
public abstract class DesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext> where TDbContext : DbContext
{
    /// <inheritdoc />
    public TDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        var connectionStringOption = new Option<string>("--connectionString", "Specifies the connection string.");
        var command = new RootCommand();

        command.AddOption(connectionStringOption);

        var parser = new Parser(command);
        var parseResult = parser.Parse(args);
        var connectionString = parseResult.GetValueForOption(connectionStringOption) ?? "Data Source=local";

        ConfigureBuilder(builder, connectionString);

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), builder.Options)!;
    }

    /// <summary>
    /// Implement this to configure the <see cref="DbContextOptionsBuilder{TContext}"/>.
    /// </summary>
    protected abstract void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString);
}