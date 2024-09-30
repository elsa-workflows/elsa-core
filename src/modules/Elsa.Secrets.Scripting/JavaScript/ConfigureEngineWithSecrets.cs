using System.Dynamic;
using System.Text.RegularExpressions;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Scripting.JavaScript;

/// A handler that configures the Jint engine with workflow variables.
[UsedImplicitly]
public partial class ConfigureEngineWithSecrets(ISecretManager secretManager, IDecryptor decryptor) : INotificationHandler<EvaluatingJavaScript>
{
    /// <inheritdoc />
    public async Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        await CopySecretsIntoEngineAsync(notification, cancellationToken);
    }
    
    private async Task CopySecretsIntoEngineAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        var expression = notification.Expression;
        var secretNames = new List<string>();
        
        foreach (Match match in SecretsRegex().Matches(expression)) 
            secretNames.Add(match.Value);

        if (secretNames.Count == 0)
            return;
        
        var filter = new SecretFilter
        {
            Names = secretNames,
            Status = SecretStatus.Active
        };
        var secrets = await secretManager.FindManyAsync(filter, cancellationToken);
        var secretsContainer = (IDictionary<string, object?>)new ExpandoObject();

        foreach (var secret in secrets)
        {
            var secretValue = await decryptor.DecryptAsync(secret.EncryptedValue, cancellationToken);
            secretsContainer[secret.Name] = secretValue;
        }

        engine.SetValue("secrets", secretsContainer);
    }

    [GeneratedRegex(@"(?<=secrets\.)\w+")]
    private static partial Regex SecretsRegex();
}