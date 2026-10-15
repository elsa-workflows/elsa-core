using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Secrets.Services;
using Microsoft.Extensions.DependencyInjection;
using ElsaSecretsExternalAuthenticationShellFeature = Elsa.ExternalAuthentication.Secrets.ShellFeatures.ElsaSecretsExternalAuthenticationFeature;

namespace Elsa.ExternalAuthentication.UnitTests.Features;

public class ElsaSecretsExternalAuthenticationFeatureTests
{
    [Fact]
    public void ShellFeatureRegistersTheManagedSecretBridge()
    {
        var services = new ServiceCollection();
        var feature = new ElsaSecretsExternalAuthenticationShellFeature();

        feature.ConfigureServices(services);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ISecretBindingResolver) &&
            descriptor.ImplementationType == typeof(ElsaSecretBindingResolver));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IManagedSecretBindingWriter) &&
            descriptor.ImplementationType == typeof(ElsaSecretBindingResolver));
    }
}
