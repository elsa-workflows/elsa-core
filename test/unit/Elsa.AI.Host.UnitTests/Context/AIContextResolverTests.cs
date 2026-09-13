using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Context;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests.Context;

public class AIContextResolverTests
{
    [Test]
    [DisplayName("Context resolver resolves workflow definition references server-side")]
    public async Task ContextResolverResolvesWorkflowDefinitionReferences()
    {
        using var provider = CreateProvider(services => services.AddSingleton<IAIContextProvider, WorkflowDefinitionContextProvider>());
        var resolver = provider.GetRequiredService<AIContextResolver>();

        var result = await resolver.ResolveAsync(new AIChatRequest
        {
            UserId = "user-1",
            Attachments =
            [
                new AIContextAttachment
                {
                    Kind = "WorkflowDefinition",
                    ReferenceId = "workflow-1"
                }
            ]
        });

        var context = await Assert.That(result).HasSingleItem();
        await Assert.That(context.Kind).IsEqualTo("WorkflowDefinition");
        await Assert.That(context.ReferenceId).IsEqualTo("workflow-1");
        await Assert.That(context.Data).IsEmpty();
    }

    [Test]
    [DisplayName("Context resolver redacts sensitive data and metadata keys")]
    public async Task ContextResolverRedactsSensitiveDataAndMetadataKeys()
    {
        using var provider = CreateProvider(services => services.AddSingleton<IAIContextProvider, SensitiveContextProvider>());
        var resolver = provider.GetRequiredService<AIContextResolver>();

        var result = await resolver.ResolveAsync(new AIChatRequest
        {
            UserId = "user-1",
            Attachments =
            [
                new AIContextAttachment
                {
                    Kind = "Sensitive"
                }
            ]
        });

        var context = await Assert.That(result).HasSingleItem();
        await Assert.That(context.Summary).IsEqualTo("Handles OAuth token refresh");
        await Assert.That(context.Data["accessToken"]!.GetValue<string>()).IsEqualTo("[redacted]");
        await Assert.That(context.Data["description"]!.GetValue<string>()).IsEqualTo("[redacted]");
        await Assert.That(context.Metadata["apiKey"]!.GetValue<string>()).IsEqualTo("[redacted]");
        await Assert.That(context.Data["displayName"]!.GetValue<string>()).IsEqualTo("visible");
    }

    [Test]
    [DisplayName("Context resolver uses the last provider for duplicate provider kinds")]
    public async Task ContextResolverUsesTheLastProviderForDuplicateProviderKinds()
    {
        using var provider = CreateProvider(services =>
        {
            services.AddSingleton<IAIContextProvider>(new DuplicateContextProvider("first"));
            services.AddSingleton<IAIContextProvider>(new DuplicateContextProvider("second"));
        });
        var resolver = provider.GetRequiredService<AIContextResolver>();

        var result = await resolver.ResolveAsync(new AIChatRequest
        {
            UserId = "user-1",
            Attachments = [new AIContextAttachment { Kind = "Duplicate" }]
        });

        var context = await Assert.That(result).HasSingleItem();
        await Assert.That(context.Summary).IsEqualTo("second");
    }

    [Test]
    [DisplayName("Context resolver prefers real providers over placeholders")]
    public async Task ContextResolverPrefersRealProvidersOverPlaceholders()
    {
        using var provider = CreateProvider(services =>
        {
            services.AddSingleton<IAIContextProvider>(new DuplicateContextProvider("real", "WorkflowDefinition"));
            services.AddSingleton<IAIContextProvider, WorkflowDefinitionContextProvider>();
        });
        var resolver = provider.GetRequiredService<AIContextResolver>();

        var result = await resolver.ResolveAsync(new AIChatRequest
        {
            UserId = "user-1",
            Attachments = [new AIContextAttachment { Kind = "WorkflowDefinition", ReferenceId = "workflow-1" }]
        });

        var context = await Assert.That(result).HasSingleItem();
        await Assert.That(context.Summary).IsEqualTo("real");
    }

    [Test]
    [DisplayName("Context resolver resolves scoped providers from request scopes")]
    public async Task ContextResolverResolvesScopedProvidersFromRequestScopes()
    {
        using var provider = CreateProvider(services => services.AddScoped<IAIContextProvider, ScopedContextProvider>());
        var resolver = provider.GetRequiredService<AIContextResolver>();

        var result = await resolver.ResolveAsync(new AIChatRequest
        {
            UserId = "user-1",
            Attachments = [new AIContextAttachment { Kind = "Scoped" }]
        });

        var context = await Assert.That(result).HasSingleItem();
        await Assert.That(context.Summary).IsEqualTo("scoped");
    }

    private static ServiceProvider CreateProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<AIContextResolver>();
        services.AddSingleton<WorkflowGroundingMapper>();
        services.AddSingleton(sp => new AIGroundingResultFormatter(MicrosoftOptions.Create(new AIHostOptions())));
        services.AddSingleton<RuntimeGroundingMapper>();
        configure(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private class SensitiveContextProvider : IAIContextProvider
    {
        public string Kind => "Sensitive";

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AIResolvedContext
            {
                Kind = Kind,
                Summary = "Handles OAuth token refresh",
                Data = new JsonObject
                {
                    ["accessToken"] = "token-value",
                    ["description"] = "Bearer eyJhbGciOi",
                    ["displayName"] = "visible"
                },
                Metadata = new JsonObject
                {
                    ["apiKey"] = "key-value"
                }
            });
        }
    }

    private class DuplicateContextProvider(string summary, string kind = "Duplicate") : IAIContextProvider
    {
        public string Kind => kind;

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AIResolvedContext
            {
                Kind = Kind,
                Summary = summary
            });
        }
    }

    private class ScopedContextProvider : IAIContextProvider
    {
        public string Kind => "Scoped";

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AIResolvedContext
            {
                Kind = Kind,
                Summary = "scoped"
            });
        }
    }
}