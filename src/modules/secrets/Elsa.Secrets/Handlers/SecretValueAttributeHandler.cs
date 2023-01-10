using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Design;
using Elsa.Events;
using Elsa.Expressions;
using Elsa.Secrets.Enrichers;
using Elsa.Secrets.Providers;
using MediatR;

namespace Elsa.Secrets.Handlers
{
    public class SecretValueAttributeHandler : INotificationHandler<DescribingActivityType>
    {
        private readonly ISecretsProvider secretsProvider;

        public SecretValueAttributeHandler(ISecretsProvider secretsProvider) {
            this.secretsProvider=secretsProvider;
        }

        public async Task Handle(DescribingActivityType notification, CancellationToken cancellationToken)
        {
            var properties = notification.ActivityType.Type.GetProperties();

            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.IsDefined(typeof(SecretValueAttribute)) == false) continue;

                var secretValue = propertyInfo.GetCustomAttribute<SecretValueAttribute>();
                var descriptor = notification.ActivityDescriptor.InputProperties.FirstOrDefault(x => x.Name == propertyInfo.Name);
                if (descriptor == null) continue;

                descriptor.UIHint = ActivityInputUIHints.Dropdown;
                if (secretValue!.ApplySecretsSyntax)
                    descriptor.DefaultSyntax = SyntaxNames.Secret;

                var secrets = await secretsProvider.GetSecretsDictionaryAsync(secretValue.Type);
                var items = secrets.Select(x => new SelectListItem(x.Key, x.Value)).ToList();
                items.Insert(0, new SelectListItem("", "empty"));
                descriptor.Options = new SelectList { Items = items };
            }
        }
    }
}