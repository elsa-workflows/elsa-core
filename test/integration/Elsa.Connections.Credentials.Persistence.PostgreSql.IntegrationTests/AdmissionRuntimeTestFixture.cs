using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

/// <summary>One actual isolated runtime host and its trusted synthetic subscription per scenario.</summary>
internal sealed class AdmissionRuntimeTestFixture(PostgreSqlConnectionsFixture fixture)
{
    public async Task RunAsync(Func<AdmissionRuntimeScenario, Task> assertion, IInterceptor? interceptor = null, bool shell = false)
    {
        await fixture.ResetSchemaAsync();
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString);
        await using var services = AdmissionRuntimeHost.CreateServices(fixture.ConnectionString, probe, shell: shell, admissionInterceptor: interceptor);
        using var tenant = AdmissionRuntimeHost.EnterTenant(services);
        await AdmissionRuntimeHost.MigrateAsync(services);
        await AdmissionRuntimeHost.BootstrapAsync(services);
        var execution = services.GetRequiredService<AdmissionExecutionService>();
        var admission = await execution.AdmitAsync(AdmissionWorkerHost.Event());
        Assert.Equal(AdmissionOutcome.Committed, admission.Outcome);
        await assertion(new(services, probe, execution, services.GetRequiredService<IAdmissionStore>(), admission.AdmissionId!));
    }
}

internal sealed record AdmissionRuntimeScenario(IServiceProvider Services, AdmissionRuntimeProbe Probe,
    AdmissionExecutionService Execution, IAdmissionStore Store, string AdmissionId);
