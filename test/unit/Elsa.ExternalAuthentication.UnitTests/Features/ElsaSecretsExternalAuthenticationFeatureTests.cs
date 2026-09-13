using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Secrets.Services;
using Microsoft.Extensions.DependencyInjection;
using ElsaSecretsExternalAuthenticationShellFeature = Elsa.ExternalAuthentication.Secrets.ShellFeatures.ElsaSecretsExternalAuthenticationFeature;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Features;

public class ElsaSecretsExternalAuthenticationFeatureTests
{
    [Test]
    public async Task ShellFeatureRegistersTheManagedSecretBridge()
    {
        var services = new ServiceCollection();
        var feature = new ElsaSecretsExternalAuthenticationShellFeature();

        feature.ConfigureServices(services);

        await Assert.That(services).Contains(descriptor =>
            descriptor.ServiceType == typeof(ISecretBindingResolver) &&
            descriptor.ImplementationType == typeof(ElsaSecretBindingResolver));
        await Assert.That(services).Contains(descriptor =>
            descriptor.ServiceType == typeof(IManagedSecretBindingWriter) &&
            descriptor.ImplementationType == typeof(ElsaSecretBindingResolver));
    }
}
