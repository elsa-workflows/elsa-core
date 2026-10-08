using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.IO.Compression;
using Elsa.Common;
using Elsa.Http;
using Elsa.Http.ContentWriters;
using Elsa.Http.Parsers;
using Elsa.Http.Options;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Elsa.Workflows.Exceptions;

namespace Elsa.Activities.UnitTests.Http;

public class WriteFileHttpResponseTests
{
    [Theory]
    [InlineData("text/plain", "test.txt")]
    [InlineData("application/pdf", "document.pdf")]
    [InlineData("image/jpeg", "photo.jpg")]
    [InlineData("application/zip", "archive.zip")]
    public async Task Should_Set_Correct_Content_Type_And_Filename(string contentType, string filename)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new(testContent);
        activity.ContentType = new(contentType);
        activity.Filename = new(filename);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal(contentType, httpContext.Response.ContentType);
        Assert.Contains($"filename={filename}", httpContext.Response.Headers.ContentDisposition.ToString());
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("")]
    public async Task Should_Handle_String_Content(string content)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        activity.Content = new(content);
        activity.Filename = new("test.txt");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        var responseContent = GetResponseContent(httpContext);
        Assert.Equal(content, responseContent);
    }

    [Fact]
    public async Task Should_Handle_Byte_Array_Content()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testBytes = "Hello World"u8.ToArray();
        
        activity.Content = new(testBytes);
        activity.Filename = new("test.bin");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        var responseBytes = GetResponseBytes(httpContext);
        Assert.Equal(testBytes, responseBytes);
    }

    [Fact]
    public async Task Should_Handle_Stream_Content()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Stream content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
        
        activity.Content = new(stream);
        activity.Filename = new("stream.txt");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        var responseContent = GetResponseContent(httpContext);
        Assert.Equal(testContent, responseContent);
    }

    [Fact]
    public async Task Should_Handle_Uri_Content()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testUri = new Uri("https://example.com/file.txt");
        
        activity.Content = new(testUri);
        activity.Filename = new("downloaded.txt");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        // The actual file download would be mocked through the IDownloadableManager
    }

    [Fact]
    public async Task Should_Handle_Multiple_Files_As_Zip()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var files = new object[]
        {
            "File 1 content",
            "File 2 content"u8.ToArray()
        };
        
        activity.Content = new(files);
        activity.Filename = new("archive.zip");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal("application/zip", httpContext.Response.ContentType);
        Assert.Contains("filename=archive.zip", httpContext.Response.Headers.ContentDisposition.ToString());
    }

    [Fact]
    public async Task Should_Return_NoContent_When_Content_Is_Null()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        activity.Content = new((object?)null!, string.Empty);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal(StatusCodes.Status204NoContent, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_ApplicationZip_When_Content_Is_Empty_Array()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        activity.Content = new(Array.Empty<object>());

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("application/zip", httpContext.Response.ContentType);
    }

    [Theory]
    [InlineData("\"12345\"")]
    [InlineData("\"abcdef\"")]
    public async Task Should_Set_Entity_Tag_Header(string entityTag)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new(testContent);
        activity.EntityTag = new(entityTag);
        activity.EnableResumableDownloads = new(true);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        
        // Strong ETags should match exactly
        Assert.Equal(entityTag, httpContext.Response.Headers.ETag.ToString());
    }

    [Fact]
    public async Task Should_Enable_Range_Processing_When_Resumable_Downloads_Enabled()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new(testContent);
        activity.EnableResumableDownloads = new(true);
        activity.EntityTag = new("\"test-etag\"");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        
        // Range processing and ETag should be handled by FileStreamResult
        Assert.True(httpContext.Response.Headers.ContainsKey("ETag"));
    }

    [Fact]
    public async Task Should_Not_Set_ETag_When_Resumable_Downloads_Disabled()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new(testContent);
        activity.EnableResumableDownloads = new(false);
        activity.EntityTag = new("\"test-etag\"");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.False(httpContext.Response.Headers.ContainsKey("ETag"));
    }

    [Theory]
    [InlineData("file.txt", "text/plain")]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("archive.zip", "application/zip")]
    [InlineData("unknown.xyz", "application/octet-stream")]
    public async Task Should_Determine_Content_Type_From_Filename_When_Not_Specified(string filename, string expectedContentType)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new(testContent);
        activity.Filename = new(filename);
        // ContentType not set, should be determined from filename

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal(expectedContentType, httpContext.Response.ContentType);
    }

    [Fact]
    public async Task Should_Use_Default_Filename_When_Not_Specified()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new(testContent);
        // Filename not set, should default to "file.bin"

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Contains("filename=file.bin", httpContext.Response.Headers.ContentDisposition.ToString());
    }

    [Fact]
    public async Task Should_Fault_When_No_HttpContext_Available()
    {
        // Arrange
        var activity = new WriteFileHttpResponse();
        var fixture = new ActivityTestFixture(activity);
        fixture.ConfigureServices(services =>
        {
            var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);
            services.AddSingleton(mockHttpContextAccessor);
            AddMockServices(services);
        });

        // Act + Assert
        await Assert.ThrowsAsync<FaultException>(() => fixture.ExecuteAsync());
    }

    [Fact] 
    public async Task Should_Handle_Downloadable_Object_With_Metadata()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var downloadable = new Downloadable
        {
            Stream = new MemoryStream("Test content"u8.ToArray()),
            Filename = "metadata-file.txt",
            ContentType = "text/plain",
            ETag = "\"metadata-etag\""
        };
        
        activity.Content = new(downloadable);

        // Act  
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal("text/plain", httpContext.Response.ContentType);
        Assert.Contains("filename=metadata-file.txt", httpContext.Response.Headers.ContentDisposition.ToString());
    }

    [Fact]
    public async Task Should_Throw_FormatException_For_Malformed_ETag()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new(testContent);
        activity.EntityTag = new("W/\"weak-etag\""); // Malformed weak ETag format
        activity.EnableResumableDownloads = new(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FormatException>(async () =>
        {
            await ExecuteActivityAsync(activity, httpContext);
        });
        
        Assert.Contains("The format of value 'W/\"weak-etag\"' is invalid", exception.Message);
    }

    // Helper Methods
    private static (WriteFileHttpResponse activity, HttpContext httpContext) CreateWriteFileHttpResponseActivity()
    {
        var activity = new WriteFileHttpResponse();
        var httpContext = CreateMockHttpContext();
        
        return (activity, httpContext);
    }

    private static DefaultHttpContext CreateMockHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        
        // Add required services for FileStreamResult
        services.AddSingleton<IActionResultExecutor<Microsoft.AspNetCore.Mvc.FileStreamResult>>(
            _ => new FileStreamResultExecutor(LoggerFactory.Create(b => b.AddConsole()))
        );
        
        // Add IContentTypeProvider mock
        services.AddSingleton(CreateContentTypeProviderMock());
        
        httpContext.RequestServices = services.BuildServiceProvider();
        httpContext.Response.Body = new MemoryStream();
        
        return httpContext;
    }

    private static IContentTypeProvider CreateContentTypeProviderMock()
    {
        var mockContentTypeProvider = Substitute.For<IContentTypeProvider>();
        mockContentTypeProvider.TryGetContentType(Arg.Any<string>(), out Arg.Any<string?>())
            .Returns(callInfo =>
            {
                var filename = callInfo.ArgAt<string>(0);
                var extension = Path.GetExtension(filename).ToLowerInvariant();
                
                var contentType = extension switch
                {
                    ".txt" => "text/plain",
                    ".pdf" => "application/pdf", 
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".zip" => "application/zip",
                    ".html" => "text/html",
                    ".css" => "text/css",
                    ".js" => "application/javascript",
                    ".json" => "application/json",
                    ".xml" => "application/xml",
                    _ => null
                };
                
                callInfo[1] = contentType;
                return contentType != null;
            });
        return mockContentTypeProvider;
    }

    private static async Task<ActivityExecutionContext> ExecuteActivityAsync(WriteFileHttpResponse activity, HttpContext httpContext)
    {
        var fixture = new ActivityTestFixture(activity);
        fixture.ConfigureServices(services =>
        {
            var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            mockHttpContextAccessor.HttpContext.Returns(httpContext);
            services.AddSingleton(mockHttpContextAccessor);
            services.AddSingleton(CreateMockHttpActivityOptions());
            AddHttpContentFactories(services);
            AddHttpContentParsers(services);
            AddMockServices(services);
            services.AddSingleton(Substitute.For<IStimulusHasher>());
            services.AddLogging();
        });

        return await fixture.ExecuteAsync();
    }

    private static Microsoft.Extensions.Options.IOptions<HttpActivityOptions> CreateMockHttpActivityOptions()
    {
        var options = new HttpActivityOptions
        {
            WriteHttpResponseSynchronously = false
        };
        var mockOptions = Substitute.For<Microsoft.Extensions.Options.IOptions<HttpActivityOptions>>();
        mockOptions.Value.Returns(options);
        return mockOptions;
    }

    private static void AddHttpContentFactories(IServiceCollection services)
    {
        services.AddSingleton<IHttpContentFactory, JsonContentFactory>();
        services.AddSingleton<IHttpContentFactory, TextContentFactory>();
        services.AddSingleton<IHttpContentFactory, XmlContentFactory>();
        services.AddSingleton<IHttpContentFactory, FormUrlEncodedHttpContentFactory>();
    }

    private static void AddHttpContentParsers(IServiceCollection services)
    {
        services.AddSingleton<IHttpContentParser, JsonHttpContentParser>();
        services.AddSingleton<IHttpContentParser, PlainTextHttpContentParser>();
        services.AddSingleton<IHttpContentParser, XmlHttpContentParser>();
        services.AddSingleton<IHttpContentParser, TextHtmlHttpContentParser>();
        services.AddSingleton<IHttpContentParser, FileHttpContentParser>();
    }

    private static void AddMockServices(IServiceCollection services)
    {
        services.AddSingleton(CreateDownloadableManagerMock());
        services.AddSingleton(CreateContentTypeProviderMock());
        services.AddSingleton(Substitute.For<IFileCacheStorageProvider>());
        services.AddSingleton(CreateFileCacheOptionsMock());
        services.AddSingleton(CreateSystemClockMock());
        services.AddSingleton(Substitute.For<IStimulusHasher>());
        services.AddLogging();
        
        RegisterZipManager(services);
    }

    private static IDownloadableManager CreateDownloadableManagerMock()
    {
        var mock = Substitute.For<IDownloadableManager>();
        mock.GetDownloadablesAsync(Arg.Any<object>(), Arg.Any<DownloadableOptions>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var content = callInfo.ArgAt<object>(0);
                return CreateMockDownloadables(content).Select(d => new Func<ValueTask<Downloadable>>(() => ValueTask.FromResult(d)));
            });
        return mock;
    }

    private static Microsoft.Extensions.Options.IOptions<HttpFileCacheOptions> CreateFileCacheOptionsMock()
    {
        var mock = Substitute.For<Microsoft.Extensions.Options.IOptions<HttpFileCacheOptions>>();
        mock.Value.Returns(new HttpFileCacheOptions { TimeToLive = TimeSpan.FromHours(1) });
        return mock;
    }

    private static ISystemClock CreateSystemClockMock()
    {
        var mock = Substitute.For<ISystemClock>();
        mock.UtcNow.Returns(DateTimeOffset.UtcNow);
        return mock;
    }

    private static void RegisterZipManager(IServiceCollection services)
    {
        var zipManagerType = typeof(WriteFileHttpResponse).Assembly.GetType("Elsa.Http.Services.ZipManager");
        if (zipManagerType != null)
        {
            services.AddSingleton(zipManagerType, CreateZipManagerInstance);
        }
        else
        {
            services.AddSingleton<object>(_ => new());
        }
    }

    private static object CreateZipManagerInstance(IServiceProvider serviceProvider)
    {
        var zipManagerType = typeof(WriteFileHttpResponse).Assembly.GetType("Elsa.Http.Services.ZipManager");
        if (zipManagerType == null) return new();

        try
        {
            var constructor = zipManagerType.GetConstructors().FirstOrDefault();
            if (constructor == null) return CreateZipManagerFallback(zipManagerType);

            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                args[i] = ResolveConstructorParameter(serviceProvider, parameters[i].ParameterType);
            }

            return constructor.Invoke(args);
        }
        catch
        {
            return CreateZipManagerFallback(zipManagerType);
        }
    }

    private static object ResolveConstructorParameter(IServiceProvider serviceProvider, Type paramType)
    {
        return paramType switch
        {
            _ when paramType == typeof(ISystemClock) => serviceProvider.GetRequiredService<ISystemClock>(),
            _ when paramType == typeof(IFileCacheStorageProvider) => serviceProvider.GetRequiredService<IFileCacheStorageProvider>(),
            _ when paramType == typeof(Microsoft.Extensions.Options.IOptions<HttpFileCacheOptions>) => serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HttpFileCacheOptions>>(),
            { IsGenericType: true } when paramType.GetGenericTypeDefinition() == typeof(ILogger<>) => CreateLoggerMock(paramType),
            _ when paramType == typeof(ILogger) => Substitute.For<ILogger>(),
            _ => CreateGenericMock(paramType)
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2060:Call to MakeGenericMethod can not be statically analyzed", Justification = "Test helper method that creates mocks dynamically for any logger type.")]
    private static object CreateLoggerMock(Type loggerType)
    {
        try
        {
            var mockLoggerMethod = typeof(Substitute).GetMethods()
                .FirstOrDefault(m => m is { Name: "For", IsGenericMethodDefinition: true } && m.GetParameters().Length == 0);

            if (mockLoggerMethod != null)
            {
                var genericMethod = mockLoggerMethod.MakeGenericMethod(loggerType);
                return genericMethod.Invoke(null, null) ?? Substitute.For<ILogger>();
            }
        }
        catch
        {
            // Fall back to basic ILogger mock
        }
        return Substitute.For<ILogger>();
    }

    private static object CreateGenericMock(Type paramType)
    {
        try
        {
            return Substitute.For([paramType], []);
        }
        catch
        {
            return null!;
        }
    }

    private static object CreateZipManagerFallback(Type zipManagerType)
    {
        return Substitute.For([zipManagerType], []);
    }

    private static IEnumerable<Downloadable> CreateMockDownloadables(object content)
    {
        switch (content)
        {
            // Simplified mock creation for different content types
            case byte[] byteArray:
                yield return new()
                {
                    Stream = new MemoryStream(byteArray),
                    ContentType = null,
                    Filename = null
                };
                break;
            case string text:
                yield return new()
                {
                    Stream = new MemoryStream(Encoding.UTF8.GetBytes(text)),
                    ContentType = null,
                    Filename = null
                };
                break;
            case Stream stream:
                // For Stream objects, return them as-is
                yield return new()
                {
                    Stream = stream,
                    ContentType = null,
                    Filename = null
                };
                break;
            case Uri uri:
                {
                    // For Uri objects, simulate downloaded content
                    var mockContent = $"Downloaded content from {uri}";
                    yield return new()
                    {
                        Stream = new MemoryStream(Encoding.UTF8.GetBytes(mockContent)),
                        ContentType = null,
                        Filename = null
                    };
                    break;
                }
            case Downloadable downloadable:
                // For Downloadable objects, return them as-is
                yield return downloadable;
                break;
            case IEnumerable<object> fileContents:
                {
                    // For collections, create a zip downloadable
                    var zipDownloadable = CreateZipDownloadable(fileContents);
                    yield return zipDownloadable;
                    break;
                }
            default:
                throw new NotSupportedException("Unsupported content type");
        }
    }

    private static Downloadable CreateZipDownloadable(IEnumerable<object> fileContents)
    {
        // Don't use 'using' here to avoid disposing the stream prematurely
        var memoryStream = new MemoryStream();
        
        try
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var fileIndex = 0;
                foreach (var fileContent in fileContents)
                {
                    var entryName = $"file{fileIndex++}.txt";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    
                    using (var entryStream = entry.Open())
                    {
                        var contentBytes = Encoding.UTF8.GetBytes(fileContent.ToString() ?? "");
                        entryStream.Write(contentBytes, 0, contentBytes.Length);
                    }
                }
            }

            memoryStream.Position = 0;
            return new()
            {
                Stream = memoryStream,
                ContentType = "application/zip",
                Filename = "archive.zip"
            };
        }
        catch
        {
            // If something goes wrong, dispose the stream to prevent leaks
            memoryStream.Dispose();
            throw;
        }
    }

    private static string GetResponseContent(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        var content = reader.ReadToEnd();
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin); // Reset stream position
        return content;
    }

    private static byte[] GetResponseBytes(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var memoryStream = new MemoryStream();
        httpContext.Response.Body.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
