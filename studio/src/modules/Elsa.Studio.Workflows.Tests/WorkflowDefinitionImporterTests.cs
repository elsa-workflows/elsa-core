using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
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

    [Fact]
    public async Task ZipImportAcceptsAnEntryAtTheExpandedSizeLimit()
    {
        using var archive = CreateArchive(("workflow.json", "{}"u8.ToArray()));
        var importer = CreateAcceptingImporter();

        var result = Assert.Single(await importer.ImportZipFileAsync(archive, new ImportOptions { MaxArchiveExpandedSize = 2 }));

        Assert.Equal("workflow.json", result.FileName);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task NestedArchivesShareTheExpansionBudget()
    {
        using var nested = CreateArchive(("nested-workflow.json", "{}"u8.ToArray()));
        var nestedBytes = nested.ToArray();
        using var archive = CreateArchive(
            ("first.json", "{}"u8.ToArray()),
            ("nested.zip", nestedBytes),
            ("last.json", "{}"u8.ToArray()));
        var importer = CreateAcceptingImporter();
        var errors = new List<Exception>();
        var options = new ImportOptions
        {
            MaxArchiveExpandedSize = nestedBytes.Length + 2,
            ErrorCallback = exception => { errors.Add(exception); return Task.CompletedTask; }
        };

        var results = await importer.ImportZipFileAsync(archive, options);

        Assert.Collection(results, first =>
        {
            Assert.Equal("first.json", first.FileName);
            Assert.Null(first.Failure);
        }, failure =>
        {
            Assert.NotNull(failure.Failure);
            Assert.Contains("expanded size limit", failure.Failure.ErrorMessage);
        });
        Assert.IsType<InvalidDataException>(Assert.Single(errors));
    }

    [Fact]
    public async Task DirectZipImportEnforcesTheUploadSizeLimit()
    {
        using var archive = CreateArchive(("workflow.json", "{}"u8.ToArray()));
        var importer = CreateAcceptingImporter();

        var result = Assert.Single(await importer.ImportZipFileAsync(archive, new ImportOptions { MaxAllowedSize = 10 }));

        Assert.NotNull(result.Failure);
        Assert.Contains("ZIP upload exceeds the size limit", result.Failure.ErrorMessage);
    }

    private static WorkflowDefinitionImporter CreateAcceptingImporter() =>
        new(new ApiProvider(DispatchProxy.Create<IWorkflowDefinitionsApi, AcceptingApi>()),
            DispatchProxy.Create<IMediator, CompletedMediator>(), new AnyWorkflowJson());

    private static MemoryStream CreateArchive(params (string Name, byte[] Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var entryStream = archive.CreateEntry(name).Open();
                entryStream.Write(content);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private class AcceptingApi : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Task.FromResult(new WorkflowDefinition());
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
