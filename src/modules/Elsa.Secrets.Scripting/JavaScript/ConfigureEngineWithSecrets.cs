using System.Dynamic;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Secrets.Management;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.Secrets.Scripting.JavaScript;

/// A handler that configures the Jint engine with secrets.
[UsedImplicitly]
public class ConfigureEngineWithSecrets(ISecretManager secretManager, IDecryptor decryptor) : INotificationHandler<EvaluatingJavaScript>
{
    /// <inheritdoc />
    public async Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        await GenerateSecretAccessorFunctions(notification, cancellationToken);
    }

    private async Task GenerateSecretAccessorFunctions(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        var secrets = (await secretManager.ListAsync(cancellationToken)).ToList();

        IDictionary<string, object?> secretsContainer = new ExpandoObject();

        foreach (var secret in secrets)
        {
            // E.g. secrets.getMySecretAsync()
            secretsContainer[$"get{secret.Name.Pascalize()}Async"] = () => decryptor.DecryptAsync(secret.EncryptedValue, cancellationToken);
        }
        
        engine.SetValue("secrets", secretsContainer);
    }
}