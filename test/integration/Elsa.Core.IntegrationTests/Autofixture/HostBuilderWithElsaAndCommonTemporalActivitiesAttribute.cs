using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Activities.Temporal;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.IntegrationTests.Extensions
{
    public class HostBuilderWithElsaAndCommonTemporalActivitiesAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBubilderUsingServicesCustomization(services => {
                services.AddElsa(elsa => {
                    CommonTemporalActivityServices.AddCommonTemporalActivities(elsa);
                });
            }, parameter);
        }
    }
}