using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Copilot.Options;
using GitHub.Copilot;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Copilot.Adapters;

public class CopilotProvider(
    IOptions<CopilotOptions> options,
    CopilotSessionEventMapper eventMapper,
    ILogger<CopilotProvider> logger) : IAIProvider
{
    public string Name => options.Value.ProviderName ?? "copilot";

    public ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default)
    {
        var providerName = request.ProviderConfiguration?.Name ?? options.Value.ProviderName ?? "copilot";

        return ValueTask.FromResult(new AISessionHandle
        {
            Id = request.ConversationId,
            ProviderSessionId = $"{providerName}:{request.ConversationId}"
        });
    }

    public async IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(
        AITurnRequest request,
        IAIProviderToolInvoker toolInvoker,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var copilotOptions = options.Value;
        await using var client = CreateClient(copilotOptions);
        await client.StartAsync(cancellationToken);

        await using var session = await CreateOrResumeSessionAsync(client, request, toolInvoker, cancellationToken);
        var events = Channel.CreateUnbounded<AIProviderEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        using var subscription = session.On<SessionEvent>(sessionEvent =>
        {
            foreach (var providerEvent in eventMapper.Map(sessionEvent))
                events.Writer.TryWrite(providerEvent);

            if (sessionEvent is SessionIdleEvent or SessionErrorEvent)
                events.Writer.TryComplete();
        });

        try
        {
            await session.SendAsync(new MessageOptions
            {
                Prompt = BuildPrompt(request),
                DisplayPrompt = request.Message
            }, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            events.Writer.TryComplete(e);
        }

        await foreach (var providerEvent in events.Reader.ReadAllAsync(cancellationToken))
            yield return providerEvent;
    }

    private CopilotClient CreateClient(CopilotOptions copilotOptions)
    {
        var clientOptions = new CopilotClientOptions
        {
            Connection = CreateRuntimeConnection(copilotOptions),
            WorkingDirectory = copilotOptions.WorkingDirectory,
            BaseDirectory = copilotOptions.BaseDirectory,
            GitHubToken = copilotOptions.GitHubToken,
            UseLoggedInUser = copilotOptions.UseLoggedInUser,
            Logger = logger
        };

        return new CopilotClient(clientOptions);
    }

    private static RuntimeConnection? CreateRuntimeConnection(CopilotOptions copilotOptions)
    {
        if (!string.IsNullOrWhiteSpace(copilotOptions.RuntimeUrl))
            return RuntimeConnection.ForUri(copilotOptions.RuntimeUrl, copilotOptions.ConnectionToken);

        if (!string.IsNullOrWhiteSpace(copilotOptions.RuntimePath) || copilotOptions.RuntimeArguments.Count > 0)
            return RuntimeConnection.ForStdio(copilotOptions.RuntimePath, copilotOptions.RuntimeArguments.ToList());

        return null;
    }

    private async Task<CopilotSession> CreateOrResumeSessionAsync(CopilotClient client, AITurnRequest request, IAIProviderToolInvoker toolInvoker, CancellationToken cancellationToken)
    {
        var providerSessionId = NormalizeSessionId(request.ProviderSessionId) ?? request.ConversationId;
        var resumeConfig = ConfigureSession(new ResumeSessionConfig
        {
            ContinuePendingWork = true,
            SuppressResumeEvent = true
        }, request, toolInvoker);

        try
        {
            return await client.ResumeSessionAsync(providerSessionId, resumeConfig, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogDebug(e, "Copilot session {ProviderSessionId} could not be resumed; creating a new session.", providerSessionId);
        }

        var createConfig = ConfigureSession(new SessionConfig
        {
            SessionId = providerSessionId
        }, request, toolInvoker);

        return await client.CreateSessionAsync(createConfig, cancellationToken);
    }

    private T ConfigureSession<T>(T config, AITurnRequest request, IAIProviderToolInvoker toolInvoker) where T : SessionConfigBase
    {
        var copilotOptions = options.Value;
        var providerConfiguration = request.ProviderConfiguration;
        var model = providerConfiguration?.Model ?? copilotOptions.Model;

        config.ClientName = "Elsa Weaver";
        config.Model = model;
        config.ReasoningEffort = copilotOptions.ReasoningEffort;
        config.Streaming = copilotOptions.EnableStreaming;
        config.IncludeSubAgentStreamingEvents = copilotOptions.IncludeSubAgentStreamingEvents;
        config.Tools = CreateTools(request.Tools, toolInvoker);
        config.AvailableTools = request.Tools.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        config.OnPermissionRequest = PermissionHandler.ApproveAll;

        if (!string.IsNullOrWhiteSpace(providerConfiguration?.Endpoint))
            config.Provider = new ProviderConfig
            {
                Type = providerConfiguration.Provider,
                BaseUrl = providerConfiguration.Endpoint,
                ModelId = model
            };

        return config;
    }

    private static ICollection<AIFunctionDeclaration> CreateTools(IReadOnlyCollection<AIToolDefinition> tools, IAIProviderToolInvoker toolInvoker) =>
        tools
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => (AIFunctionDeclaration)new ElsaCopilotToolFunction(x, toolInvoker))
            .ToList();

    private static string BuildPrompt(AITurnRequest request)
    {
        if (request.Context.Count == 0)
            return request.Message;

        var prompt = new StringBuilder();
        prompt.AppendLine(request.Message);
        prompt.AppendLine();
        prompt.AppendLine("Elsa context references resolved by the server:");

        foreach (var context in request.Context)
        {
            prompt.AppendLine();
            prompt.AppendLine($"- Kind: {context.Kind}");
            prompt.AppendLine($"  ReferenceId: {context.ReferenceId}");
            if (!string.IsNullOrWhiteSpace(context.Summary))
                prompt.AppendLine($"  Summary: {context.Summary}");
            if (context.Data.Count > 0)
                prompt.AppendLine($"  Data: {context.Data}");
        }

        return prompt.ToString();
    }

    private static string? NormalizeSessionId(string? providerSessionId)
    {
        if (string.IsNullOrWhiteSpace(providerSessionId))
            return null;

        var separatorIndex = providerSessionId.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex < 0 ? providerSessionId : providerSessionId[(separatorIndex + 1)..];
    }
}
