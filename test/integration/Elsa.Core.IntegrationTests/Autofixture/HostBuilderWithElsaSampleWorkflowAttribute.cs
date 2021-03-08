using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Activities.UserTask.Activities;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class HostBuilderWithElsaSampleWorkflowAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBubilderUsingServicesCustomization(services => {
                services
                    .AddElsa(elsa => {
                        elsa.AddActivity<UserTask>();
                        elsa.AddWorkflow<SampleWorkflow>();
                    })
                    ;
            }, parameter);
        }
    }
}