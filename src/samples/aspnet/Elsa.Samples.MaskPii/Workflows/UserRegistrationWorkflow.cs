using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Samples.MaskPii.Activities;
using Elsa.Samples.MaskPii.Models;

namespace Elsa.Samples.MaskPii.Workflows;

public class UserRegistrationWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            // Build an endpoint that receives user credentials.
            .HttpEndpoint(httpEndpoint => httpEndpoint
                .WithPath("/users/signup")
                .WithMethod(HttpMethods.Post)
                .WithReadContent()
                .WithTargetType<User>())

            // Store the user in the database;
            .Then<StoreUser>(storeUser => storeUser
                .WithUser(context => context.GetInput<HttpRequestModel>()!.GetBody<User>() with { }));
    }
}