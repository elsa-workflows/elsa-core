using System.Net;
using Elsa.Activities.UnitTests.Http.Helpers;
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
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Http;

public class SendHttpRequestSecretExpressionTests(ITestOutputHelper testOutputHelper)
{
    private const string SecretName = "api:authorization";
    private const string SecretValue = "Bearer resolved-token";

    [Fact(DisplayName = "SendHttpRequest resolves Authorization from Secret expression without persisting the secret")]
    public async Task ResolvesAuthorizationSecretExpressionWithoutPersistingSecret()
    {
        var capturedRequests = new List<HttpRequestMessage>();
        var fixture = CreateFixture(CreateCapturingHandler(capturedRequests));
        var activity = new SendHttpRequest
        {
            Url = new(new Uri("https://api.example.com/secure")),
            Method = new("GET"),
            Authorization = new(SecretExpression.Create(new SecretReference(SecretName, SecretTypeNames.Text, "production"))),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>()
        };

        await fixture.BuildAsync();

        var workflowJson = fixture.Services.GetRequiredService<IWorkflowSerializer>().Serialize(Workflow.FromActivity(activity));
        Assert.Contains(SecretName, workflowJson);
        Assert.DoesNotContain(SecretValue, workflowJson);

        var result = await fixture.RunActivityAsync(activity);

        var capturedRequest = Assert.Single(capturedRequests);
        Assert.NotNull(capturedRequest.Headers.Authorization);
        Assert.Equal(SecretValue, capturedRequest.Headers.Authorization.ToString());

        var activityContext = Assert.Single(result.Journal.ActivityExecutionContexts, x => x.Activity == activity);
        Assert.False(activityContext.ActivityState.ContainsKey(nameof(SendHttpRequestBase.Authorization)));

        var workflowStateJson = fixture.Services.GetRequiredService<IWorkflowStateSerializer>().Serialize(result.WorkflowState);
        Assert.DoesNotContain(SecretValue, workflowStateJson);
    }

    private WorkflowTestFixture CreateFixture(HttpMessageHandler handler)
    {
        return new WorkflowTestFixture(testOutputHelper)
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
        public Task<string> ResolveAsync(string name, CancellationToken cancellationToken = default)
        {
            Assert.Equal(SecretName, name);
            return Task.FromResult(SecretValue);
        }

        public Task<string> ResolveAsync(SecretReference reference, CancellationToken cancellationToken = default)
        {
            Assert.Equal(new SecretReference(SecretName, SecretTypeNames.Text, "production"), reference);
            return Task.FromResult(SecretValue);
        }
    }
}
