using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Sql.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Providers;

namespace Elsa.Secrets.Handlers; 

public class SecretsValueHandler : ISendHttpRequestAuthorizationHeaderHandler, ISqlConnectionStringHandler {
    private readonly ISecretsProvider _secretsProvider;

    public SecretsValueHandler(ISecretsProvider secretsProvider) {
        _secretsProvider = secretsProvider;
    }

    public async Task<string> EvaluateStoredValue(string originalValue) {
        if (originalValue == null || !originalValue.StartsWith(Constants.SecretRefPrefix)) {
            return originalValue;
        }
        var secret = await _secretsProvider.GetSecretById(originalValue.Substring(Constants.SecretRefPrefix.Length));
        return secret;
    }
}