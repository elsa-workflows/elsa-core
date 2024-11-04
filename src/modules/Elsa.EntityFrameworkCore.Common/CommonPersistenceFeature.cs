using Elsa.EntityFrameworkCore.EntityHandlers;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore;

/// <inheritdoc />
public class CommonPersistenceFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<IEntitySavingHandler, ApplyTenantId>();
        Services.AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>();
        Services.AddScoped<IEntityModelCreatingHandler, SetupForSqlite>();
    }
}