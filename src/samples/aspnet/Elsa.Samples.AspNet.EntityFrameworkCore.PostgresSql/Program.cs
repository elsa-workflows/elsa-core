using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Identity.Entities;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using System.Security.Claims;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddElsa(elsa =>
{
    var dbContextOptions = new ElsaDbContextOptions();
    string postgresConnectionString = configuration.GetConnectionString("Postgres")!;
    string schema = configuration.GetConnectionString("Schema")!;

    if (!string.IsNullOrEmpty(schema))
    {
        dbContextOptions.SchemaName = schema;
        dbContextOptions.MigrationsAssemblyName = typeof(Program).Assembly.GetName().Name;
        dbContextOptions.AdditionnalEntityConfigurations = async (modelBuilder, serviceProvider) =>
        {
            //Add global filter on DbContext to split data between tenants
            UserManager<User> userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            IHttpContextAccessor httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            var userId = httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
                return;

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return;

            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                //IEnumerable<IModelCreatingDbContextStrategy> modelCreatingDbContextStrategies = _dbContextStrategies
                //    .OfType<IModelCreatingDbContextStrategy>()
                //    .Where(strategy => strategy.CanExecute(modelBuilder, entityType));

                //foreach (IModelCreatingDbContextStrategy modelCreatingDbContextStrategy in modelCreatingDbContextStrategies)
                //    modelCreatingDbContextStrategy.Execute(modelBuilder, entityType);

                if (entityType.ClrType.IsAssignableTo(typeof(Entity)))
                {
                    ParameterExpression parameter = Expression.Parameter(entityType.ClrType);

                    Expression<Func<Entity, bool>> filterExpr = entity => entity.TenantId == user.TenantId;
                    Expression body = ReplacingExpressionVisitor.Replace(filterExpr.Parameters[0], parameter, filterExpr.Body);
                    LambdaExpression lambdaExpression = Expression.Lambda(body, parameter);

                    entityType.SetQueryFilter(lambdaExpression);
                }
            }

        };
    }

    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(postgresConnectionString, dbContextOptions)));
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UsePostgreSql(postgresConnectionString, dbContextOptions)));

    elsa
        .UseHttp()
        .AddFastEndpointsAssembly<Program>()
        .UseWorkflowsApi();

    elsa.AddWorkflowsFrom<Program>();
});

builder.Services.SwaggerDocument(options =>
{
    options.DocumentSettings = documentSetting =>
    {
        documentSetting.Title = "Elsa API";
        documentSetting.Version = "v1";
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseWorkflows();
app.UseWorkflowsApi();

if (!app.Environment.IsProduction())
{
    app.UseOpenApi();
    app.UseSwaggerUi3();
    app.UseReDoc();
}

app.Run();
