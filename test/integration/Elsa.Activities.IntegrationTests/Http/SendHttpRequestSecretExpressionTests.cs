using System.Net;
using Elsa.Activities.IntegrationTests.Http.Helpers;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Expressions;
using Elsa.Secrets.Models;
using Elsa.Secrets.Providers;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.IntegrationTests.Http;

public class SendHttpRequestSecretExpressionTests
{
    private const string SecretName = "api:authorization";
    private const string SecretValue = "Bearer resolved-token";

    [Test]
    [DisplayName("SendHttpRequest resolves Authorization from Secret expression without persisting the secret")]
    public async Task ResolvesAuthorizationSecretExpressionWithoutPersistingSecret()
    {
        var capturedRequests = new List<HttpRequestMessage>();
        await using var fixture = CreateFixture(CreateCapturingHandler(capturedRequests));
        var activity = new SendHttpRequest
        {
            Url = new(new Uri("https://api.example.com/secure")),
            Method = new("GET"),
            Authorization = new(SecretExpression.Create(new SecretReference(SecretName, SecretTypeNames.Text, "production"))),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>()
        };

        await fixture.BuildAsync();

        var workflowJson = fixture.Services.GetRequiredService<IWorkflowSerializer>().Serialize(Workflow.FromActivity(activity));
        await Assert.That(workflowJson).Contains(SecretName, StringComparison.CurrentCulture);

        await Assert.That(workflowJson).DoesNotContain(SecretValue, StringComparison.CurrentCulture);


        var result = await fixture.RunActivityAsync(activity);

        var capturedRequest = (await Assert.That(capturedRequests).HasSingleItem())!;

        var authorization = (await Assert.That(capturedRequest.Headers.Authorization).IsNotNull())!;
        await Assert.That(authorization.ToString()).IsEqualTo(SecretValue);


        var activityContext = (await Assert.That(result.Journal.ActivityExecutionContexts).HasSingleItem(x => x.Activity == activity))!;

        await Assert.That(activityContext.ActivityState.ContainsKey(nameof(SendHttpRequestBase.Authorization))).IsFalse();


        var workflowStateJson = fixture.Services.GetRequiredService<IWorkflowStateSerializer>().Serialize(result.WorkflowState);
        await Assert.That(workflowStateJson).DoesNotContain(SecretValue, StringComparison.CurrentCulture);

    }

    private WorkflowTestFixture CreateFixture(HttpMessageHandler handler)
    {
        return new WorkflowTestFixture(TestContext.Current!.Output.StandardOutput)
            .ConfigureServices(services =>
            {
                services.AddSingleton<ISecretResolver>(new TestSecretResolver());
                services.AddSingleton<IExpressionDescriptorProvider, SecretExpressionDescriptorProvider>();
            })
            .ConfigureElsa(elsa => elsa.UseHttp(http =>
            {
                http.HttpClientBuilder = builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler);
            }));
    }

    private static HttpMessageHandler CreateCapturingHandler(ICollection<HttpRequestMessage> capturedRequests) =>
        new TestHttpMessageHandler((request, _) =>
        {
            capturedRequests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

    private class TestSecretResolver : ISecretResolver
    {
        public async Task<string> ResolveAsync(string name, CancellationToken cancellationToken = default)
        {
            await Assert.That(name).IsEqualTo(SecretName);

            return SecretValue;
        }

        public async Task<string> ResolveAsync(SecretReference reference, CancellationToken cancellationToken = default)
        {
            await Assert.That(reference).IsEqualTo(new SecretReference(SecretName, SecretTypeNames.Text, "production"));

            return SecretValue;
        }
    }
}
