namespace Elsa.Agents;

/// <summary>
/// Invokes an agent using the Microsoft Agent Framework.
/// </summary>
public interface IAgentInvoker
{
    /// <summary>
    /// Invokes an agent using the Microsoft Agent Framework.
    /// </summary>
    Task<InvokeAgentResult> InvokeAsync(InvokeAgentRequest request);
}
