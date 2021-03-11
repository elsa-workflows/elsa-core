using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Activities.UserTask.Activities;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Persistence.MongoDb.Extensions;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class HostBuilderWithElsaSampleWorkflowAndMongoDbAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBubilderUsingServicesCustomization(services => {
                services
                    .AddElsa(elsa => {
                        elsa.UseMongoDbPersistence(opts => {
                            opts.ConnectionString = "mongodb://localhost:27017";
                            opts.DatabaseName = "IntegrationTests";
                        });
                        elsa.AddActivity<UserTask>();
                        elsa.AddWorkflow<PersistableWorkflow>();
                    })
                    ;
            }, parameter);
        }
    }
}