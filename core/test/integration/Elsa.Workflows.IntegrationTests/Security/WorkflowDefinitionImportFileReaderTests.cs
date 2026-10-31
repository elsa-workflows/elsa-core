using System.IO.Compression;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.ImportFiles;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Elsa.Workflows.IntegrationTests.Security;

public class WorkflowDefinitionImportFileReaderTests
{
    private readonly IApiSerializer _apiSerializer = Substitute.For<IApiSerializer>();

    public WorkflowDefinitionImportFileReaderTests()
    {
        _apiSerializer.Deserialize<WorkflowDefinitionModel>(Arg.Any<string>())
            .Returns(call => new WorkflowDefinitionModel { DefinitionId = call.Arg<string>() });
    }

    [Fact]
    public async Task ReadAsync_CollectsAllJsonModelsBeforeImportPreflight()
    {
        var files = new FormFileCollection
        {
            CreateJsonFile("plain-a"),
            CreateZipFile(("workflow-b.json", "zip-b"), ("workflow-c.json", "zip-c"), ("notes.txt", "ignored"))
        };

        var models = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => false, CancellationToken.None);

        Assert.Equal(["plain-a", "zip-b", "zip-c"], models.Select(x => x.DefinitionId));
    }

    [Fact]
    public async Task ReadAsync_StopsReadingZipEntries_WhenResponseHasStarted()
    {
        var hasResponseStarted = false;
        var files = new FormFileCollection
        {
            CreateZipFile(("workflow-a.json", "zip-a"), ("workflow-b.json", "zip-b"))
        };

        _apiSerializer.Deserialize<WorkflowDefinitionModel>(Arg.Any<string>())
            .Returns(call =>
            {
                hasResponseStarted = true;
                return new WorkflowDefinitionModel { DefinitionId = call.Arg<string>() };
            });

        var models = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => hasResponseStarted, CancellationToken.None);

        Assert.Equal(["zip-a"], models.Select(x => x.DefinitionId));
    }

    private static IFormFile CreateJsonFile(string content)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, leaveOpen: true);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;

        return new FormFile(stream, 0, stream.Length, "files", $"{content}.json")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/json"
        };
    }

    private static IFormFile CreateZipFile(params (string Name, string Content)[] entries)
    {
        var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "files", "workflows.zip")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/zip"
        };
    }
}
