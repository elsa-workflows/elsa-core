using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Secrets.Providers;
using Elsa.Services.Models;

namespace Elsa.Secrets.Handlers; 

public class SecretsHandler : IExpressionHandler {
    private readonly ISecretsProvider _secretsProvider;
    public string Syntax => "Secret";

    public SecretsHandler(ISecretsProvider secretsProvider) {
        _secretsProvider = secretsProvider;
    }

    public async Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, CancellationToken cancellationToken) {
        return await _secretsProvider.GetSecretByName(expression);
    }
}