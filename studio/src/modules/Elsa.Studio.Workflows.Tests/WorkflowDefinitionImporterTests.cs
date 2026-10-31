using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class WorkflowDefinitionImporterTests
{
    [Fact]
    public async Task ABodylessForbiddenResponse_IsReportedAsThePermissionGuidance()
    {
        var api = DispatchProxy.Create<IWorkflowDefinitionsApi, RefusingApi>();
        var mediator = DispatchProxy.Create<IMediator, CompletedMediator>();
        var importer = new WorkflowDefinitionImporter(new ApiProvider(api), mediator, new AnyWorkflowJson());

        var result = await importer.ImportJsonAsync("workflow.json", "{}", null);

        Assert.Equal(AuthorizationFailureExtensions.ForbiddenMessage, result.Failure!.ErrorMessage);
    }

    private class RefusingApi : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw ApiExceptions.Create(HttpStatusCode.Forbidden);
    }

    private class CompletedMediator : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Task.CompletedTask;
    }

    private class AnyWorkflowJson : IWorkflowJsonDetector
    {
        public bool IsWorkflowSchema(string json) => true;
    }

    private class ApiProvider(IWorkflowDefinitionsApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://localhost");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromResult((T)(object)api);
    }
}
