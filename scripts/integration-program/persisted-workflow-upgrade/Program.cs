using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Common.Multitenancy;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Activities.SetOutput;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// This identical file is compiled against each released baseline and the verified candidate.
var phase = args[0];
var spec = JsonNode.Parse(File.ReadAllText(args[1]))!.AsObject();
var receiptPath = args[2];
var receipt = new JsonObject { ["phase"] = phase, ["passed"] = false };
using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(180));
var ct = deadline.Token;
var options = new JsonSerializerOptions { WriteIndented = true };
string Get(string key) => spec[key]!.GetValue<string>();
void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
JsonNode Json(object? value) => JsonSerializer.SerializeToNode(value)!;

try
{
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    services.AddSingleton<IConfiguration>(new ConfigurationManager());
    services.AddElsa(elsa => elsa
        .UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite($"Data Source={Get("management")}")))
        .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite($"Data Source={Get("runtime")}"))));
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var sp = scope.ServiceProvider;
    receipt["tenant_id"] = Tenant.DefaultTenantId;
    receipt["migrations_before"] = await Migrations(sp, ct);
    var tenants = sp.GetRequiredService<ITenantService>();
    await tenants.ActivateTenantsAsync(ct);
    try
    {
        receipt["migrations_after"] = await Migrations(sp, ct);
        var runtime = sp.GetRequiredService<IWorkflowRuntime>();
        if (phase == "suspend")
        {
            var types = new[] { typeof(string), typeof(int), typeof(bool) };
            var names = new[] { "text", "number", "flag" };
            var inputs = names.Select((name, index) => new InputDefinition
            {
                Name = name, Type = types[index], StorageDriverType = typeof(WorkflowInstanceStorageDriver)
            }).ToArray();
            var sentinel = new Variable<string>("sentinel", "", Get("sentinel_id"))
                .WithStorageDriver<WorkflowInstanceStorageDriver>();
            Input<object?> FromInput(InputDefinition input) => new(new Expression("Input", input));
            var root = new Sequence
            {
                Id = "upgrade-sequence",
                Activities =
                {
                    new SetVariable { Id = "capture-input", Variable = sentinel, Value = FromInput(inputs[0]) },
                    new Event(Get("event_name")) { Id = "upgrade-event" },
                    new SetOutput { Id = "output-text", OutputName = new("text"), OutputValue = new(sentinel) },
                    new SetOutput { Id = "output-number", OutputName = new("number"), OutputValue = FromInput(inputs[1]) },
                    new SetOutput { Id = "output-flag", OutputName = new("flag"), OutputValue = FromInput(inputs[2]) }
                }
            };
            var publisher = sp.GetRequiredService<IWorkflowDefinitionPublisher>();
            var definition = await publisher.NewAsync(root, ct);
            definition.Id = Get("definition_version_id");
            definition.DefinitionId = Get("definition_id");
            definition.Version = 1;
            definition.Name = "Persisted package upgrade proof";
            definition.Inputs = inputs;
            definition.Outputs = names.Select((name, index) => new OutputDefinition { Name = name, Type = types[index] }).ToArray();
            definition.Variables = new[] { sentinel };
            Require(definition.OriginalSource == null, "OriginalSource must remain null");
            var published = await publisher.PublishAsync(definition, ct);
            Require(published.Succeeded, "Definition publication failed: " + JsonSerializer.Serialize(published));
            var client = await runtime.CreateClientAsync(Get("instance_id"), ct);
            await client.CreateInstanceAsync(new CreateWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(definition.Id),
                CorrelationId = Get("correlation_id"),
                Input = new Dictionary<string, object> { ["text"] = Get("text"), ["number"] = 37, ["flag"] = true }
            }, ct);
            var response = await client.RunInstanceAsync(new RunWorkflowInstanceRequest { IncludeWorkflowOutput = true }, ct);
            Require(response.Status == WorkflowStatus.Running && response.SubStatus == WorkflowSubStatus.Suspended && response.Incidents.Count == 0,
                "Baseline failed to suspend without incidents: " + JsonSerializer.Serialize(response));
            receipt["state"] = await ReadState(false, null);
        }
        else
        {
            Require(phase is "resume" or "verify", "Unknown phase");
            var baseline = JsonNode.Parse(File.ReadAllText(Get("baseline_receipt")))!.AsObject();
            Require(baseline["passed"]!.GetValue<bool>(), "Baseline receipt did not pass");
            var original = baseline["state"]!.AsObject();
            var bookmarkId = original["bookmark"]!["Id"]!.GetValue<string>();
            receipt["before"] = await ReadState(phase == "verify", original);
            if (phase == "resume")
            {
                var client = await runtime.CreateClientAsync(Get("instance_id"), ct);
                // No inputs or variables are supplied: only the original persisted bookmark.
                var response = await client.RunInstanceAsync(new RunWorkflowInstanceRequest
                {
                    BookmarkId = bookmarkId, IncludeWorkflowOutput = true
                }, ct);
                Require(response.Status == WorkflowStatus.Finished && response.SubStatus == WorkflowSubStatus.Finished && response.Incidents.Count == 0,
                    "Candidate failed to finish without incidents: " + JsonSerializer.Serialize(response));
                Require(JsonNode.DeepEquals(Json(response.Output), spec["expected"]), "Response output value/type mismatch");
                receipt["state"] = await ReadState(true, original);
            }
            else receipt["state"] = receipt["before"]!.DeepClone();
        }

        async Task<JsonObject> ReadState(bool finished, JsonObject? original)
        {
            var definition = await sp.GetRequiredService<IWorkflowDefinitionStore>().FindAsync(new WorkflowDefinitionFilter { Id = Get("definition_version_id") }, ct);
            Require(definition != null, "Persisted definition is missing");
            Require(definition!.DefinitionId == Get("definition_id") && definition.Version == 1 && definition.OriginalSource == null,
                "Persisted definition identity/source mismatch");
            var expectedTypes = new Dictionary<string, Type> { ["text"] = typeof(string), ["number"] = typeof(int), ["flag"] = typeof(bool) };
            Require(definition.Inputs.Count == 3 && definition.Inputs.All(x => expectedTypes.TryGetValue(x.Name, out var type) && x.Type == type && x.StorageDriverType == typeof(WorkflowInstanceStorageDriver))
                && definition.Inputs.Select(x => x.Name).Distinct().Count() == 3, "Typed durable input metadata is missing");
            Require(definition.Outputs.Count == 3 && definition.Outputs.All(x => expectedTypes.TryGetValue(x.Name, out var type) && x.Type == type)
                && definition.Outputs.Select(x => x.Name).Distinct().Count() == 3, "Typed output metadata is missing");
            Require(definition.Variables.Count == 1 && definition.Variables.Single().Id == Get("sentinel_id")
                && definition.Variables.Single().StorageDriverType == typeof(WorkflowInstanceStorageDriver), "Durable variable metadata mismatch");
            var graph = JsonNode.Parse(definition.StringData!)!;
            var expectedActivities = new[] { ("capture-input", "Elsa.SetVariable"), ("upgrade-event", "Elsa.Event"), ("output-text", "Elsa.SetOutput"), ("output-number", "Elsa.SetOutput"), ("output-flag", "Elsa.SetOutput") };
            Require(graph["id"]!.GetValue<string>() == "upgrade-sequence" && graph["type"]!.GetValue<string>() == "Elsa.Sequence" && graph["version"]!.GetValue<int>() == 1,
                "Root activity identity/version mismatch");
            var activities = graph["activities"]!.AsArray();
            Require(activities.Count == expectedActivities.Length, "Unexpected activity graph");
            for (var index = 0; index < activities.Count; index++)
                Require(activities[index]!["id"]!.GetValue<string>() == expectedActivities[index].Item1 && activities[index]!["type"]!.GetValue<string>() == expectedActivities[index].Item2
                    && activities[index]!["version"]!.GetValue<int>() == 1, "Activity identity/version/order mismatch");
            var instance = await sp.GetRequiredService<IWorkflowInstanceStore>().FindAsync(new WorkflowInstanceFilter { Id = Get("instance_id") }, ct);
            Require(instance != null && instance.WorkflowState != null, "Persisted instance/state is missing");
            var state = instance!.WorkflowState ?? throw new InvalidOperationException("Loaded state is null");
            Require(state.Id == Get("instance_id") && state.DefinitionId == Get("definition_id") && state.DefinitionVersionId == Get("definition_version_id")
                && state.DefinitionVersion == 1 && state.CorrelationId == Get("correlation_id") && instance.IncidentCount == 0 && state.Incidents.Count == 0,
                "Loaded state is empty, default or has changed identity/incidents");
            Require(JsonNode.DeepEquals(Json(state.Input), spec["expected"]), "Persisted input value/type mismatch");
            var properties = Json(state.Properties);
            // Instance storage keeps scoped variables in the owning activity context.
            // Finished workflows no longer have that active context; its output proves the read.
            var sentinel = finished ? Json(state.Output)["text"] : Json(state.ActivityExecutionContexts.Single(x => x.ParentContextId == null).Properties)["Variables"]?[Get("sentinel_id")];
            Require(sentinel?.GetValue<string>() == Get("text"), "Durable input-derived sentinel mismatch");
            var bookmarks = (await sp.GetRequiredService<IBookmarkStore>().FindManyAsync(new BookmarkFilter { WorkflowInstanceId = state.Id }, ct)).ToList();
            var result = new JsonObject
            {
                ["instance_id"] = state.Id, ["definition_id"] = state.DefinitionId, ["definition_version_id"] = state.DefinitionVersionId,
                ["correlation_id"] = state.CorrelationId, ["definition_version"] = state.DefinitionVersion,
                ["definition_json_sha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(definition.StringData!))).ToLowerInvariant(),
                ["input"] = Json(state.Input), ["output"] = Json(state.Output), ["properties"] = properties,
                ["sentinel"] = sentinel!.DeepClone(),
                ["input_metadata"] = Json(definition.Inputs.Select(x => new { name = x.Name, type = x.Type.FullName, driver = x.StorageDriverType!.FullName })),
                ["output_metadata"] = Json(definition.Outputs.Select(x => new { name = x.Name, type = x.Type.FullName })),
                ["activity_graph"] = graph,
                ["status"] = state.Status.ToString(), ["sub_status"] = state.SubStatus.ToString(),
                ["state_bookmark_count"] = state.Bookmarks.Count, ["stored_bookmark_count"] = bookmarks.Count,
                ["input_clr_types"] = Json(state.Input.ToDictionary(x => x.Key, x => x.Value.GetType().FullName)),
                ["output_clr_types"] = Json(state.Output.ToDictionary(x => x.Key, x => x.Value.GetType().FullName))
            };
            if (finished)
            {
                Require(state.Status == WorkflowStatus.Finished && state.SubStatus == WorkflowSubStatus.Finished && instance.Status == WorkflowStatus.Finished,
                    "Finished state was not durable");
                Require(JsonNode.DeepEquals(Json(state.Output), spec["expected"]), "Durable output value/type mismatch");
                Require(state.Bookmarks.Count == 0 && bookmarks.Count == 0, "Original bookmark was not consumed");
            }
            else
            {
                Require(state.Status == WorkflowStatus.Running && state.SubStatus == WorkflowSubStatus.Suspended && state.Bookmarks.Count == 1 && bookmarks.Count == 1,
                    "Exactly one durable suspension bookmark required");
                var bookmark = state.Bookmarks.Single();
                var stored = bookmarks.Single();
                Require(bookmark.ActivityId == "upgrade-event" && bookmark.Id == stored.Id && bookmark.Hash == stored.Hash && stored.WorkflowInstanceId == state.Id,
                    "Event bookmark identity mismatch");
                result["bookmark"] = Json(bookmark);
                result["stored_bookmark"] = Json(stored);
                if (original != null)
                    Require(JsonNode.DeepEquals(result["bookmark"], original["bookmark"]) && JsonNode.DeepEquals(result["stored_bookmark"], original["stored_bookmark"]),
                        "Original bookmark payload/identity changed before resume");
            }
            if (original != null)
                foreach (var key in new[] { "instance_id", "definition_id", "definition_version_id", "correlation_id", "definition_version", "definition_json_sha256", "input", "sentinel", "input_metadata", "output_metadata", "activity_graph" })
                    Require(JsonNode.DeepEquals(result[key], original[key]), "Original persisted field changed: " + key);
            return result;
        }
    }
    finally { await tenants.DeactivateTenantsAsync(ct); }
    receipt["assemblies"] = Json(AppDomain.CurrentDomain.GetAssemblies()
        .Where(x => x.GetName().Name!.StartsWith("Elsa", StringComparison.Ordinal) && !x.IsDynamic)
        .OrderBy(x => x.GetName().Name).Select(x => new
        {
            name = x.GetName().Name, identity = x.FullName, location = x.Location,
            informational_version = x.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(x.Location))).ToLowerInvariant()
        }));
    receipt["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
    receipt["inherited_provider_warning_configuration"] = "PersistenceFeatureBase already ignores PendingModelChangesWarning; fixture adds no override";
    receipt["passed"] = true;
}
catch (Exception error)
{
    receipt["error"] = error.ToString();
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}
finally { File.WriteAllText(receiptPath, receipt.ToJsonString(options)); }

static async Task<JsonObject> Migrations(IServiceProvider services, CancellationToken ct)
{
    var result = new JsonObject();
    await Capture<ManagementElsaDbContext>("management");
    await Capture<RuntimeElsaDbContext>("runtime");
    return result;
    async Task Capture<T>(string name) where T : DbContext
    {
        await using var context = await services.GetRequiredService<IDbContextFactory<T>>().CreateDbContextAsync(ct);
        result[name] = new JsonObject
        {
            ["context"] = typeof(T).FullName, ["data_source"] = context.Database.GetDbConnection().DataSource,
            ["applied"] = JsonSerializer.SerializeToNode(await context.Database.GetAppliedMigrationsAsync(ct)),
            ["pending"] = JsonSerializer.SerializeToNode(await context.Database.GetPendingMigrationsAsync(ct))
        };
    }
}
