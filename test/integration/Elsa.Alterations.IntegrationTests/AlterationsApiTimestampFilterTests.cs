using System.Net;
using System.Net.Http.Json;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Endpoints.Alterations.DryRun;
using Elsa.Common;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SubmitEndpoint = Elsa.Alterations.Endpoints.Alterations.Submit.Submit;

namespace Elsa.Alterations.IntegrationTests;

[Collection(EndpointSecurityTestCollection.Name)]
public class AlterationsApiTimestampFilterTests : IAsyncLifetime
{
    private WebApplication? _app;
    private bool _wasSecurityEnabled;
    private HttpClient? _httpClient;
    private HttpClient HttpClient => _httpClient ?? throw new InvalidOperationException("The test HTTP client has not been initialized.");

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = false;

        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();

            builder.Services.AddFastEndpoints(o =>
            {
                o.Assemblies = [typeof(DryRun).Assembly];
                o.Filter = endpointType => endpointType == typeof(DryRun) || endpointType == typeof(SubmitEndpoint);
            });
            builder.Services.AddSingleton(Substitute.For<IWorkflowInstanceFinder>());
            builder.Services.AddSingleton(Substitute.For<IAlterationPlanScheduler>());
            builder.Services.AddSingleton(Substitute.For<IAlterationRunner>());
            builder.Services.AddSingleton(Substitute.For<IAlteredWorkflowDispatcher>());
            builder.Services.AddSingleton(Substitute.For<IAlterationPlanStore>());
            builder.Services.AddSingleton(Substitute.For<IAlterationJobStore>());
            builder.Services.AddSingleton(Substitute.For<IWorkflowDispatcher>());
            builder.Services.AddSingleton(Substitute.For<IWorkflowInstanceStore>());
            builder.Services.AddSingleton(Substitute.For<IIdentityGenerator>());
            builder.Services.AddSingleton(Substitute.For<ISystemClock>());
            builder.Services.AddLogging();

            _app = builder.Build();
            _app.UseFastEndpoints();

            await _app.StartAsync();
            _httpClient = _app.GetTestClient();
        }
        catch
        {
            EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
            await DisposeAppAsync(false);
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        await DisposeAppAsync(true);
    }

    private async Task DisposeAppAsync(bool stopApp)
    {
        _httpClient?.Dispose();
        _httpClient = null;

        if (_app == null)
            return;

        if (stopApp)
            await _app.StopAsync();

        await _app.DisposeAsync();
        _app = null;
    }

    [Theory]
    [InlineData("/alterations/dry-run")]
    [InlineData("/alterations/submit")]
    public async Task Post_WithInjectedTimestampFilterColumn_ReturnsBadRequest(string path)
    {
        var response = await HttpClient.PostAsJsonAsync(path, CreateRequest(path));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid timestamp filter column", body);
    }

    [Fact]
    public async Task Submit_WithNullFilter_DoesNotReturnServerError()
    {
        var response = await HttpClient.PostAsJsonAsync("/alterations/submit", new { filter = (object?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/alterations/dry-run")]
    [InlineData("/alterations/submit")]
    public async Task Post_WithNullTimestampFilter_ReturnsBadRequest(string path)
    {
        var response = await HttpClient.PostAsJsonAsync(path, CreateRequestWithNullTimestampFilter(path));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Timestamp filter at index 0 must be specified.", body);
    }

    [Theory]
    [InlineData("/alterations/dry-run")]
    [InlineData("/alterations/submit")]
    public async Task Post_WithMultipleInvalidTimestampFilters_ReturnsAllValidationErrors(string path)
    {
        var response = await HttpClient.PostAsJsonAsync(path, CreateRequestWithMultipleInvalidTimestampFilters(path));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Timestamp filter at index 0: Timestamp filter column must be specified.", body);
        Assert.Contains("Timestamp filter at index 1 must be specified.", body);
    }

    private static object CreateRequest(string path)
    {
        var filter = new
        {
            timestampFilters = new[]
            {
                new
                {
                    column = "CreatedAt == @0 || Id != null",
                    @operator = TimestampFilterOperator.Is,
                    timestamp = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero)
                }
            }
        };

        return path == "/alterations/submit"
            ? new { filter }
            : filter;
    }

    private static object CreateRequestWithNullTimestampFilter(string path)
    {
        var filter = new
        {
            timestampFilters = new object?[] { null }
        };

        return path == "/alterations/submit"
            ? new { filter }
            : filter;
    }

    private static object CreateRequestWithMultipleInvalidTimestampFilters(string path)
    {
        var filter = new
        {
            timestampFilters = new object?[]
            {
                new
                {
                    column = " ",
                    @operator = TimestampFilterOperator.Is,
                    timestamp = new DateTimeOffset(2026, 5, 20, 10, 0, 0, TimeSpan.Zero)
                },
                null
            }
        };

        return path == "/alterations/submit"
            ? new { filter }
            : filter;
    }
}
