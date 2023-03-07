namespace Elsa.Expressions.Contracts;

public interface IExpressionSyntaxRegistryPopulator
{
    ValueTask PopulateRegistryAsync(CancellationToken cancellationToken);
}