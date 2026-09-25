using Microsoft.SemanticKernel.ChatCompletion;

namespace Elsa.Agents;

/// <summary>
/// Represents a request to invoke an agent.
/// </summary>
public class InvokeAgentRequest
{
    /// <summary>
    /// Gets or sets the name of the agent to invoke.
    /// </summary>
    public required string AgentName { get; set; }

    /// <summary>
    /// Gets or sets the input parameters for the agent.
    /// </summary>
    public IDictionary<string, object?> Input { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Gets or sets the chat history. If null, a new chat history will be created.
    /// </summary>
    public ChatHistory? ChatHistory { get; set; }
    
    /// <summary>
    /// Gets or sets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
