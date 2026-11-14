using System.IO.Compression;
using System.Text.Json;
using Elsa.Testing.Shared;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.ImportFiles;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Security;

public class WorkflowDefinitionImportFileReaderTests
{
    private readonly IApiSerializer _apiSerializer = Substitute.For<IApiSerializer>();
    private readonly ITestOutputHelper _testOutputHelper;

    public WorkflowDefinitionImportFileReaderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _apiSerializer.Deserialize<WorkflowDefinitionModel>(Arg.Any<string>())
            .Returns(call => new WorkflowDefinitionModel { DefinitionId = call.Arg<string>() });
    }

    [Fact]
    public async Task ReadAsync_CollectsAllJsonModelsBeforeImportPreflight()
    {
        var files = new FormFileCollection
        {
            CreateJsonFile("plain-a"),
            CreateZipFile(("workflow-b.json", "zip-b"), ("workflow-c.JSON", "zip-c"), ("notes.txt", "ignored"))
        };

        var result = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => false, CancellationToken.None);

        Assert.Null(result.ErrorMessage);
        Assert.Equal(["plain-a", "zip-b", "zip-c"], result.Models.Select(x => x.DefinitionId));
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

        var result = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => hasResponseStarted, CancellationToken.None);

        Assert.Null(result.ErrorMessage);
        Assert.Equal(["zip-a"], result.Models.Select(x => x.DefinitionId));
    }

    [Fact]
    public async Task ReadAsync_MalformedJson_RejectsEntireBatchAndStopsReading()
    {
        _apiSerializer.Deserialize<WorkflowDefinitionModel>("{ not json")
            .Returns(_ => throw new JsonException("Parser details must not be exposed"));
        var files = new FormFileCollection
        {
            CreateJsonFile("valid"),
            CreateJsonFile("{ not json", "bad.json"),
            CreateJsonFile("later")
        };

        var result = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => false, CancellationToken.None);

        Assert.Empty(result.Models);
        Assert.Equal("bad.json is not a valid workflow definition.", result.ErrorMessage);
        _apiSerializer.Received(1).Deserialize<WorkflowDefinitionModel>("valid");
        _apiSerializer.DidNotReceive().Deserialize<WorkflowDefinitionModel>("later");
    }

    [Fact]
    public async Task ReadAsync_NullDefinition_IdentifiesFile()
    {
        _apiSerializer.Deserialize<WorkflowDefinitionModel>("null").Returns((WorkflowDefinitionModel)null!);
        var files = new FormFileCollection { CreateJsonFile("null", "empty.json") };

        var result = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => false, CancellationToken.None);

        Assert.Empty(result.Models);
        Assert.Equal("empty.json is not a valid workflow definition.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("""{ "version": "abc" }""")]
    [InlineData("""{ "version": "99999999999" }""")]
    public async Task ReadAsync_WrongTypeValue_IsTreatedLikeMalformedJson(string json)
    {
        // Arrange
        var realSerializer = new TestApplicationBuilder(_testOutputHelper).Build().GetRequiredService<IApiSerializer>();
        var files = new FormFileCollection { CreateJsonFile(json, "wrong-type.json") };

        // Act
        var result = await WorkflowDefinitionImportFileReader.ReadAsync(files, realSerializer, () => false, CancellationToken.None);

        // Assert
        Assert.Empty(result.Models);
        Assert.Equal("wrong-type.json is not a valid workflow definition.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("not a zip")]
    [InlineData("This upload is not a ZIP archive and has no central directory.")]
    public async Task ReadAsync_NonZipUpload_IdentifiesArchive(string content)
    {
        var file = CreateJsonFile(content, "bundle.zip");
        file.ContentType = "application/octet-stream";
        var files = new FormFileCollection { file };

        var result = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => false, CancellationToken.None);

        Assert.Empty(result.Models);
        Assert.Equal("bundle.zip is not a valid workflow archive.", result.ErrorMessage);
        _apiSerializer.DidNotReceive().Deserialize<WorkflowDefinitionModel>(Arg.Any<string>());
    }

    [Fact]
    public async Task ReadAsync_MalformedZipEntry_IdentifiesEntryAndArchiveAndDiscardsEarlierModels()
    {
        _apiSerializer.Deserialize<WorkflowDefinitionModel>("{ not json")
            .Returns(_ => throw new JsonException("Parser details must not be exposed"));
        var files = new FormFileCollection
        {
            CreateJsonFile("plain"),
            CreateZipFile(("valid.json", "zip-valid"), ("flows/bad.json", "{ not json"), ("later.json", "later"))
        };

        var result = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => false, CancellationToken.None);

        Assert.Empty(result.Models);
        Assert.Equal("flows/bad.json in workflows.zip is not a valid workflow definition.", result.ErrorMessage);
        _apiSerializer.Received(1).Deserialize<WorkflowDefinitionModel>("zip-valid");
        _apiSerializer.DidNotReceive().Deserialize<WorkflowDefinitionModel>("later");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadAsync_CorruptZipEntry_IdentifiesArchive(bool corruptCompressedData)
    {
        var archive = CreateZipFile(("workflow.json", "zip-model"));
        using var buffer = new MemoryStream();
        await using (var stream = archive.OpenReadStream())
            await stream.CopyToAsync(buffer);

        var bytes = buffer.ToArray();
        // The local header precedes the entry name and extra fields, then the compressed data.
        var dataOffset = 30 + BitConverter.ToUInt16(bytes, 26) + BitConverter.ToUInt16(bytes, 28);
        bytes[corruptCompressedData ? dataOffset : 0] = corruptCompressedData ? (byte)0x07 : (byte)0;
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "files", "broken.zip")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/zip"
        };
        var files = new FormFileCollection { CreateJsonFile("valid"), file };

        var result = await WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => false, CancellationToken.None);

        Assert.Empty(result.Models);
        Assert.Equal("broken.zip is not a valid workflow archive.", result.ErrorMessage);
        _apiSerializer.DidNotReceive().Deserialize<WorkflowDefinitionModel>("zip-model");
    }

    [Fact]
    public async Task ReadAsync_SerializerServiceFailureInZip_IsNotConvertedToValidationFailure()
    {
        var exception = new InvalidDataException("Service failure");
        _apiSerializer.Deserialize<WorkflowDefinitionModel>("zip-model").Returns(_ => throw exception);
        var files = new FormFileCollection { CreateZipFile(("workflow.json", "zip-model")) };

        var actual = await Assert.ThrowsAsync<InvalidDataException>(() =>
            WorkflowDefinitionImportFileReader.ReadAsync(files, _apiSerializer, () => false, CancellationToken.None));

        Assert.Same(exception, actual);
    }

    private static FormFile CreateJsonFile(string content, string? fileName = null)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, leaveOpen: true);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;

        return new FormFile(stream, 0, stream.Length, "files", fileName ?? $"{content}.json")
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
