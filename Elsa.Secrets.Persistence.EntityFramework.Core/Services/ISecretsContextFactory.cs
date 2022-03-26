using Elsa.Persistence.EntityFramework.Core.Services;

namespace Elsa.Secrets.Persistence.EntityFramework.Core.Services
{
    public interface ISecretsContextFactory : IContextFactory<SecretsContext>
    {
    }
}
