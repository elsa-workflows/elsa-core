using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Persistence.MongoDb.Extensions;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class HostBuilderWithPersistableWorkflowAndMongoDbAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBuilderUsingServicesCustomization(services => {
                services
                    .AddElsa(elsa => {
                        elsa
                            .UseMongoDbPersistence(opts => {
                                opts.ConnectionString = "mongodb://localhost:27017";
                                opts.DatabaseName = "IntegrationTests";
                            })
                            .AddPersistableWorkflow();
                    })
                    ;
            }, parameter);
        }
    }
}