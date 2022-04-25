namespace Elsa.Management.Services;

public interface IExpressionSyntaxRegistryPopulator
{
    ValueTask PopulateRegistryAsync(CancellationToken cancellationToken);
}