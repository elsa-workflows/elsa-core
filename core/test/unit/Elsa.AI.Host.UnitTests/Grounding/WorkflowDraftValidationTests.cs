using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Elsa.AI.Host.Services;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.UnitTests.Grounding;

public class WorkflowDraftValidationTests
{
    [Fact(DisplayName = "Draft validation reports stale baseline")]
    public void DraftValidationReportsStaleBaseline()
    {
        var validator = new WorkflowDraftValidationService(new ServiceCollection().BuildServiceProvider());

        var diagnostics = validator.Validate(new JsonObject { ["name"] = "Draft" }, "old-version", "new-version");

        Assert.Contains(diagnostics, x => x.Code == "baseline.stale");
    }

    [Fact(DisplayName = "Draft validation reports missing draft")]
    public void DraftValidationReportsMissingDraft()
    {
        var validator = new WorkflowDraftValidationService(new ServiceCollection().BuildServiceProvider());

        var diagnostics = validator.Validate([]);

        Assert.Contains(diagnostics, x => x.Code == "draft.empty");
    }

    [Fact(DisplayName = "Draft validation reports unavailable activity descriptors")]
    public void DraftValidationReportsUnavailableActivityDescriptors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IActivityRegistry>(new TestActivityRegistry(new ActivityDescriptor { TypeName = "Elsa.WriteLine", Name = "WriteLine", Version = 1 }));
        using var serviceProvider = services.BuildServiceProvider();
        var validator = new WorkflowDraftValidationService(serviceProvider);
        var draft = new JsonObject
        {
            ["activities"] = new JsonArray
            {
                new JsonObject { ["id"] = "installed", ["type"] = "Elsa.WriteLine" },
                new JsonObject { ["id"] = "missing", ["type"] = "Elsa.Missing" }
            }
        };

        var diagnostics = validator.Validate(draft);

        Assert.Contains(diagnostics, x => x.Code == "activity.unavailable" && x.Message.Contains("Elsa.Missing"));
        Assert.DoesNotContain(diagnostics, x => x.Message.Contains("Elsa.WriteLine"));
    }

    private class TestActivityRegistry(params ActivityDescriptor[] descriptors) : IActivityRegistry
    {
        private readonly List<ActivityDescriptor> _descriptors = descriptors.ToList();

        public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IEnumerable<ActivityDescriptor>>(_descriptors);

        public void Add(Type providerType, ActivityDescriptor descriptor) => _descriptors.Add(descriptor);
        public void Remove(Type providerType, ActivityDescriptor descriptor) => _descriptors.Remove(descriptor);
        public IEnumerable<ActivityDescriptor> ListAll() => _descriptors;
        public IEnumerable<ActivityDescriptor> ListByProvider(Type providerType) => _descriptors;
        public ActivityDescriptor? Find(string type) => _descriptors.FirstOrDefault(x => string.Equals(x.TypeName, type, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Name, type, StringComparison.OrdinalIgnoreCase));
        public ActivityDescriptor? Find(string type, int version) => _descriptors.FirstOrDefault(x => (string.Equals(x.TypeName, type, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Name, type, StringComparison.OrdinalIgnoreCase)) && x.Version == version);
        public ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate) => _descriptors.FirstOrDefault(predicate);
        public IEnumerable<ActivityDescriptor> FindMany(Func<ActivityDescriptor, bool> predicate) => _descriptors.Where(predicate);
        public void Register(ActivityDescriptor descriptor) => _descriptors.Add(descriptor);
        public Task RegisterAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RegisterAsync(IEnumerable<Type> activityTypes, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RefreshDescriptorsAsync(IEnumerable<IActivityProvider> activityProviders, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RefreshDescriptorsAsync(IActivityProvider activityProvider, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Clear() => _descriptors.Clear();
        public void ClearProvider(Type providerType) => _descriptors.Clear();
    }
}
