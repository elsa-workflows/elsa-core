namespace Elsa.Secrets.Management;

public interface IAlgorithmProvider
{
    Task<IEnumerable<AlgorithmDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}