using System.Net.Http.Headers;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.Extensions;
using Elsa.Identity.Providers;
using Elsa.Testing.Shared;
using Elsa.Workflows.Services;
using FluentStorage;
using Hangfire.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;
using static Elsa.Api.Client.RefitSettingsHelper;

namespace Elsa.Workflows.Api.ComponentTests;

[UsedImplicitly]
public class WorkflowServerWebAppFactoryFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:13.3-alpine")
        .WithDatabase("elsa")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public ITestOutputHelper? TestOutputHelper { get; set; }

    public TClient CreateApiClient<TClient>()
    {
        var client = CreateClient();
        client.BaseAddress = new Uri(client.BaseAddress!, "/elsa/api");
        return RestService.For<TClient>(client, CreateRefitSettings());
    }

    public HttpClient CreateHttpWorkflowClient()
    {
        var client = CreateClient();
        client.BaseAddress = new Uri(client.BaseAddress!, "/workflows/");
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbConnectionString = _dbContainer.GetConnectionString();

        Program.ConfigureForTest = elsa =>
        {
            elsa.UseDefaultAuthentication(defaultAuthentication => defaultAuthentication.UseAdminApiKey());
            elsa.UseFluentStorageProvider(sp =>
            {
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;
                var workflowsDirectorySegments = new[]
                {
                    assemblyDirectory, "Scenarios"
                };
                var workflowsDirectory = Path.Join(workflowsDirectorySegments);
                return StorageFactory.Blobs.DirectoryFiles(workflowsDirectory);
            });
            elsa.UseWorkflowManagement(management =>
            {
                management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(dbConnectionString));
            });
            elsa.UseWorkflows(workflows =>
            {
                if (TestOutputHelper != null)
                    workflows.WithStandardOutStreamProvider(_ => new StandardOutStreamProvider(new XunitConsoleTextWriter(TestOutputHelper)));
            });
        };

        builder.ConfigureTestServices(services =>
        {
            if (TestOutputHelper != null)
                services.AddLogging(logging => logging.AddProvider(new XunitLoggerProvider(TestOutputHelper)).SetMinimumLevel(LogLevel.Debug));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", AdminApiKeyProvider.DefaultApiKey);
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