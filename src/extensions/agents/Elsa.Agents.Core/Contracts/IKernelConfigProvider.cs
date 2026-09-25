namespace Elsa.Agents;

public interface IKernelConfigProvider
{
    Task<KernelConfig> GetKernelConfigAsync(CancellationToken cancellationToken = default);
}