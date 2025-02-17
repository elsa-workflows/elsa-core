using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Sql.Activities;
using Elsa.Sql.Contracts;
using Elsa.Sql.Factory;
using Elsa.Sql.Implimentations;
using Elsa.Sql.Services;
using Elsa.Sql.UIHints;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Sql.Features;

/// <summary>
/// Setup SQL client features
/// </summary>
public class SqlFeature : FeatureBase
{
    /// <summary>
    /// Set a callback to configure <see cref="ClientStore"/>.
    /// </summary>
    public Action<ClientStore> Clients { get; set; } = _ => { };

    /// <summary>
    ///  <inheritdoc/>
    /// </summary>
    /// <param name="module"></param>
    public SqlFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override void Configure()
    {
        Module.AddActivitiesFrom<SqlFeature>();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override void Apply()
    {
        Services
            .AddSingleton(provider =>
            {
                ClientStore clientRegistry = new();
                Clients.Invoke(clientRegistry);
                return clientRegistry;
            })
            .AddSingleton<ISqlClientFactory, SqlClientFactory>()

            // Providers
            .AddScoped<IPropertyUIHandler, SqlCodeOptionsProvider>()
            .AddScoped<IPropertyUIHandler, SqlClientsDropDownProvider>()
            .AddScoped<ISqlClientNamesProvider, SqlClientNamesProvider>();

    }
}