namespace Elsa.Expressions.Services;

public interface IExpressionSyntaxRegistryPopulator
{
    ValueTask PopulateRegistryAsync(CancellationToken cancellationToken);
}