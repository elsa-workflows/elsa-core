using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class HostBuilderWithElsaAndQuartzAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBuilderUsingServicesCustomization(services => {
                services.AddElsa(elsa => {
                    elsa.AddQuartzTemporalActivities();
                });
            }, parameter);
        }
    }
}