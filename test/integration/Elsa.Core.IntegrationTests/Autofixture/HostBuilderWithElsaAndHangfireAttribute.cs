using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.IntegrationTests.Extensions
{
    public class HostBuilderWithElsaAndHangfireAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBuilderUsingServicesCustomization(services => {
                services.AddElsa(elsa => {
                    elsa.AddHangfireTemporalActivities(config => config.UseInMemoryStorage());
                });
            }, parameter);
        }
    }
}