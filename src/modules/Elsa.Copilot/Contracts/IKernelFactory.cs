using Microsoft.SemanticKernel;

namespace Elsa.Copilot.Contracts;

public interface IKernelFactory
{
    IKernelBuilder CreateChatCompletionKernelBuilder();
}