using System.Text.Json;
using Microsoft.SemanticKernel;

namespace Elsa.Agents;

public record InvokeAgentResult(AgentConfig Function, ChatMessageContent ChatMessageContent)
{


}