namespace Elsa.Agents;

public interface IAgentServiceProvider
{
    string Name { get; }
    void ConfigureKernel(KernelBuilderContext context);
}