using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.Extensions;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.WorkflowProviders.BlobStorage.Providers;
using FluentStorage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Elsa.Workflows.Api.ComponentTests;

public class WorkflowServerTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:13.3-alpine")
        .WithDatabase("elsa")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbConnectionString = _dbContainer.GetConnectionString();
        
        builder.ConfigureTestServices(services =>
        {
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;
            var workflowsDirectory = new[]
            {
                assemblyDirectory, "Workflows"
            };
            services.RemoveWhere(x => x.ServiceType == typeof(IBlobStorageProvider));
            services.AddScoped<IBlobStorageProvider>(sp => new BlobStorageProvider(StorageFactory.Blobs.DirectoryFiles(Path.Combine(workflowsDirectory))));
            
            // services.ConfigureElsa(elsa =>
            // {
            //     
            //     
            //     elsa.UseWorkflowManagement(management =>
            //     {
            //         management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(dbConnectionString));
            //     });
            // });
        });
        
        base.ConfigureWebHost(builder);
    }

    Task IAsyncLifetime.InitializeAsync()
    {
        return _dbContainer.StartAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return _dbContainer.StopAsync();
    }
}