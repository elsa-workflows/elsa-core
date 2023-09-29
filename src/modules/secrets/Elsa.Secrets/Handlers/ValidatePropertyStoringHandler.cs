using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Secrets.Providers;
using Elsa.Services.Workflows;
using MediatR;

namespace Elsa.Secrets.Handlers;

public class ValidatePropertyExposureHandler : INotificationHandler<ValidatePropertyExposure>
{
    private readonly Regex _fullyQualifiedName = new Regex("(?<Type>[^:]+):(?<Name>.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private readonly ISecretsProvider _secretsProvider;

    public ValidatePropertyExposureHandler(ISecretsProvider secretsProvider)
    {
        _secretsProvider = secretsProvider;
    }

    public async Task Handle(ValidatePropertyExposure notification, CancellationToken cancellationToken)
    {
        var propProvider = notification.WorkflowBlueprint.ActivityPropertyProviders.GetProvider(notification.ActivityId, notification.PropertyName);
        var expressionProvider = propProvider as ExpressionActivityPropertyValueProvider;
        
        if (expressionProvider is not { Syntax: "Secret" })
        {
            return;
        }
        
        Match m;
        if ((m = _fullyQualifiedName.Match(expressionProvider.Expression)).Success)
        {
            if (await _secretsProvider.IsSecretValueSensitiveData(m.Groups["Type"].Value, m.Groups["Name"].Value))
            {
                notification.PreventPropertyExposure();
            }
        }
        else
        {
            if (await _secretsProvider.IsSecretValueSensitiveData(expressionProvider.Expression))
            {
                notification.PreventPropertyExposure();
            }
        }
    }
}