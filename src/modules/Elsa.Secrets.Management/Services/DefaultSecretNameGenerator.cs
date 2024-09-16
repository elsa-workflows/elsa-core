namespace Elsa.Secrets.Management;

public class DefaultSecretNameGenerator(ISecretNameValidator validator) : ISecretNameGenerator
{
    public async Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 1000;
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            var name = $"Secret {++attempt}";
            var isUnique = await validator.IsNameUniqueAsync(name, null, cancellationToken);

            if (isUnique)
                return name;
        }

        throw new Exception($"Failed to generate a unique workflow name after {maxAttempts} attempts.");
    }
}