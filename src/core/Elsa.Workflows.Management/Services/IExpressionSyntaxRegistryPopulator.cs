namespace Elsa.Workflows.Management.Services;

public interface IExpressionSyntaxRegistryPopulator
{
    ValueTask PopulateRegistryAsync(CancellationToken cancellationToken);
}