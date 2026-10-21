using System.Text.Json;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NativeCompensation.E1;

internal static class Program
{
    internal const string InstanceId = "e1-synthetic-instance";
    internal const string VersionId = "e1-definition-v1";

    public static async Task<int> Main(string[] args)
    {
        var directory = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(directory);
        var participant = args.Length > 2 ? args[2] : "http://127.0.0.1:1";
        var ambient = args.Contains("--ambient");
        using var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices(services =>
            {
                services.AddSingleton(new Barriers(args.Contains("--barriers")));
                services.AddNotificationHandler<CommitObserver, WorkflowStateCommitted>(provider => new(provider.GetRequiredService<Barriers>()));
                services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(participant) });
                services.AddDataProtection().SetApplicationName("Elsa.NativeCompensation.E1")
                    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(directory, "keys")));
                services.AddElsa(elsa => elsa
                    .AddActivity<ProbeScope>()
                    .AddActivity<ParticipantEffect>()
                    .AddActivity<Reject>()
                    .AddActivity<Handoff>()
                    .UseWorkflowManagement(management => management.UseWorkflowInstances(instances =>
                        instances.UseEntityFrameworkCore(ef => ef.UseSqlite($"Data Source={Path.Combine(directory, "elsa.db")}"))))
                    .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlite($"Data Source={Path.Combine(directory, "elsa.db")}");
                        ef.UseWorkflowCommitTransaction = ambient;
                    })));
            }).Build();

        try
        {
            await host.StartAsync();
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            await services.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
            var store = services.GetRequiredService<IWorkflowInstanceStore>();
            var existing = await store.FindAsync(new WorkflowInstanceFilter { Id = InstanceId });
            if (args[0] == "inspect")
            {
                Events.Write("inspect", new { found = existing != null, state = existing?.WorkflowState });
                return 0;
            }
            if (args[0] == "resave")
            {
                await store.SaveAsync(existing ?? throw new InvalidOperationException("Expected saved instance."));
                Events.Write("resaved", new { existing.Id, stateId = existing.WorkflowState?.Id, propertyCount = existing.WorkflowState?.Properties.Count });
                return 0;
            }
            if (args[0] == "stale")
            {
                var stale = existing ?? throw new InvalidOperationException("Expected saved instance.");
                var current = (await store.FindAsync(new WorkflowInstanceFilter { Id = InstanceId }))!;
                var currentJournal = Journal.Read(current.WorkflowState.Properties);
                currentJournal.Revision += 10;
                currentJournal.OwnerToken = "synthetic-owner-b";
                current.WorkflowState.Properties[Journal.PropertyKey] = JsonSerializer.SerializeToElement(currentJournal);
                await store.SaveAsync(current);
                var staleJournal = Journal.Read(stale.WorkflowState.Properties);
                staleJournal.Revision++;
                stale.WorkflowState.Properties[Journal.PropertyKey] = JsonSerializer.SerializeToElement(staleJournal);
                await store.SaveAsync(stale);
                var canonical = (await store.FindAsync(new WorkflowInstanceFilter { Id = InstanceId }))!;
                Events.Write("stale-writer", new { newerRevision = currentJournal.Revision, newerOwner = currentJournal.OwnerToken,
                    staleAccepted = true, canonical = Journal.Read(canonical.WorkflowState.Properties) });
                return 0;
            }

            var workflow = new Workflow(new ProbeScope())
            {
                Id = "workflow", Identity = new WorkflowIdentity("e1-definition", 1, Environment.GetEnvironmentVariable("E1_GRAPH_VERSION") ?? VersionId)
            };
            var runner = services.GetRequiredService<IWorkflowRunner>();
            var result = existing == null
                ? await runner.RunAsync(workflow, new RunWorkflowOptions { WorkflowInstanceId = InstanceId })
                : await runner.RunAsync(workflow, existing.WorkflowState);
            Events.Write("result", new
            {
                status = result.WorkflowState.Status.ToString(), subStatus = result.WorkflowState.SubStatus.ToString(),
                incidents = result.WorkflowState.Incidents.Count,
                faults = result.WorkflowState.ActivityExecutionContexts.Select(x => new { x.Id, x.FaultCount }),
                journal = Journal.Read(result.WorkflowState.Properties)
            });
            return 0;
        }
        catch (Exception exception)
        {
            Events.Write("error", new { type = exception.GetType().FullName, exception.Message });
            return 2;
        }
        finally
        {
            await host.StopAsync();
        }
    }
}

internal static class Events
{
    internal static void Write(string kind, object value) => Console.WriteLine(JsonSerializer.Serialize(new { kind, pid = Environment.ProcessId, value }));
}

internal sealed class Barriers(bool enabled)
{
    internal async Task ReachAsync(string name, WorkflowExecutionContext context)
    {
        if (!enabled)
        {
            return;
        }
        Events.Write("barrier", new { name, journal = Journal.Read(context.Properties) });
        if (await Console.In.ReadLineAsync() != "continue")
        {
            throw new InvalidOperationException("Controller disconnected at barrier.");
        }
    }
}
