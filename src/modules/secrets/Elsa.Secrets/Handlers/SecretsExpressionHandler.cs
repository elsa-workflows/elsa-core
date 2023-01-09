using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Secrets.Providers;
using Elsa.Services.Models;

namespace Elsa.Secrets.Handlers
{

    public class SecretsExpressionHandler : IExpressionHandler
    {
        private Regex fullyQualifiedName = new Regex("(?<Type>[^:]+):(?<Name>.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private readonly ISecretsProvider _secretsProvider;
        public string Syntax => "Secret";

        public SecretsExpressionHandler(ISecretsProvider secretsProvider)
        {
            _secretsProvider = secretsProvider;
        }

        public async Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            Match m;
            if ((m = fullyQualifiedName.Match(expression)).Success)
                return await _secretsProvider.GetSecretByNameAsync(m.Groups["Type"].Value, m.Groups["Name"].Value);

            return await _secretsProvider.GetSecretByNameAsync(expression);
        }
    }
}