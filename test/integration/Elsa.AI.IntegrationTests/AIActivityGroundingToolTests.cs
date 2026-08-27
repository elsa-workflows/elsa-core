using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.IntegrationTests;

public class AIActivityGroundingToolTests
{
    [Fact(DisplayName = "Activity grounding tools search and return installed descriptors")]
    public async Task ActivityGroundingToolsSearchAndReturnInstalledDescriptors()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IActivityRegistry>(new TestActivityRegistry(
            new ActivityDescriptor
            {
                TypeName = "Elsa.Http.HttpEndpoint",
                Namespace = "Elsa.Http",
                Name = "HttpEndpoint",
                DisplayName = "HTTP Endpoint",
                Category = "HTTP",
                Description = "Receives HTTP requests",
                Version = 1,
                IsStart = true
            }));
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAIToolRegistry>();
        using var tool = await registry.FindAsync("activities.search", new AIToolQuery { ActorId = "user-1" });

        var result = await tool!.ExecuteAsync(new AIToolExecutionContext
        {
            ActorId = "user-1",
            ConversationId = "conversation-1",
            Arguments = new JsonObject { ["query"] = "http", ["trigger"] = true }
        });

        Assert.Equal(AIToolInvocationStatus.Completed, result.Status);
        Assert.Equal(1, result.Data["returned"]!.GetValue<int>());
        var item = result.Data["items"]!.AsArray()[0]!.AsObject();
        Assert.Equal("Elsa.Http.HttpEndpoint", item["type"]!.GetValue<string>());
        Assert.True(item["isTrigger"]!.GetValue<bool>());
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
