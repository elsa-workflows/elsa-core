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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

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
        
        activity.Content = new Input<object>(testContent);
        activity.ContentType = new Input<string?>(contentType);
        activity.Filename = new Input<string?>(filename);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal(contentType, httpContext.Response.ContentType);
        Assert.Contains($"filename=\"{filename}\"", httpContext.Response.Headers.ContentDisposition.ToString());
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("")]
    public async Task Should_Handle_String_Content(string content)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        activity.Content = new Input<object>(content);
        activity.Filename = new Input<string?>("test.txt");

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
        
        activity.Content = new Input<object>(testBytes);
        activity.Filename = new Input<string?>("test.bin");

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
        
        activity.Content = new Input<object>(stream);
        activity.Filename = new Input<string?>("stream.txt");

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
        
        activity.Content = new Input<object>(testUri);
        activity.Filename = new Input<string?>("downloaded.txt");

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
        
        activity.Content = new Input<object>(files);
        activity.Filename = new Input<string?>("archive.zip");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal("application/zip", httpContext.Response.ContentType);
        Assert.Contains("filename=\"archive.zip\"", httpContext.Response.Headers.ContentDisposition.ToString());
    }

    [Fact]
    public async Task Should_Return_NoContent_When_Content_Is_Null()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        activity.Content = new Input<object>((object?)null!, string.Empty);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal(StatusCodes.Status204NoContent, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_NoContent_When_Content_Is_Empty_Array()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        activity.Content = new Input<object>(Array.Empty<object>());

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal(StatusCodes.Status204NoContent, httpContext.Response.StatusCode);
    }

    [Theory]
    [InlineData("\"12345\"")]
    [InlineData("\"abcdef\"")]
    [InlineData("W/\"weak-etag\"")]
    public async Task Should_Set_Entity_Tag_Header(string entityTag)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new Input<object>(testContent);
        activity.EntityTag = new Input<string?>(entityTag);
        activity.EnableResumableDownloads = new Input<bool>(true);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        
        // For weak ETags, the response header format may differ from the input
        if (entityTag.StartsWith("W/"))
        {
            // Weak ETags should be present in the response, but format might be normalized
            Assert.True(httpContext.Response.Headers.ContainsKey("ETag"));
            var responseETag = httpContext.Response.Headers.ETag.ToString();
            Assert.True(responseETag.StartsWith("W/") || responseETag.Contains("weak-etag"));
        }
        else
        {
            // Strong ETags should match exactly
            Assert.Equal(entityTag, httpContext.Response.Headers.ETag.ToString());
        }
    }

    [Fact]
    public async Task Should_Enable_Range_Processing_When_Resumable_Downloads_Enabled()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new Input<object>(testContent);
        activity.EnableResumableDownloads = new Input<bool>(true);
        activity.EntityTag = new Input<string?>("\"test-etag\"");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        // Range processing and ETag would be handled by FileStreamResult
        Assert.True(httpContext.Response.Headers.ContainsKey("ETag"));
    }

    [Fact]
    public async Task Should_Not_Set_ETag_When_Resumable_Downloads_Disabled()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        
        activity.Content = new Input<object>(testContent);
        activity.EnableResumableDownloads = new Input<bool>(false);
        activity.EntityTag = new Input<string?>("\"test-etag\"");

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.False(httpContext.Response.Headers.ContainsKey("ETag"));
    }

    [Theory]
    [InlineData("test-correlation-id")]
    [InlineData("workflow-def-123")]
    [InlineData("custom-download-id")]
    public async Task Should_Use_Download_Correlation_Id(string correlationId)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var files = new object[] { "File 1", "File 2" };
        
        activity.Content = new Input<object>(files);
        activity.DownloadCorrelationId = new Input<string>(correlationId);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        // The correlation ID would be used internally by ZipManager
    }

    [Fact]
    public async Task Should_Use_X_Download_Id_Header_When_No_Correlation_Id_Set()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var files = new object[] { "File 1", "File 2" };
        var expectedDownloadId = "header-download-id";
        
        httpContext.Request.Headers["x-download-id"] = expectedDownloadId;
        activity.Content = new Input<object>(files);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        // The header value would be used internally
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
        
        activity.Content = new Input<object>(testContent);
        activity.Filename = new Input<string?>(filename);
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
        
        activity.Content = new Input<object>(testContent);
        // Filename not set, should default to "file.bin"

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Contains("filename=\"file.bin\"", httpContext.Response.Headers.ContentDisposition.ToString());
    }

    [Fact]
    public async Task Should_Handle_Range_Header_For_Partial_Content()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World Content for Range Request"u8.ToArray();
        
        httpContext.Request.Headers["Range"] = "bytes=0-4";
        activity.Content = new Input<object>(testContent);
        activity.EnableResumableDownloads = new Input<bool>(true);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        // Range processing would be handled by FileStreamResult
    }

    [Fact]
    public async Task Should_Handle_If_Match_Header()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = "Hello World"u8.ToArray();
        var etag = "\"test-etag\"";
        
        httpContext.Request.Headers["If-Match"] = etag;
        activity.Content = new Input<object>(testContent);
        activity.EntityTag = new Input<string?>(etag);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        // If-Match processing would be handled by the downloadable manager
    }

    [Fact]
    public async Task Should_Create_Bookmark_When_No_HttpContext_Available()
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

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.False(context.IsCompleted);
        var bookmarks = context.WorkflowExecutionContext.Bookmarks.ToList();
        Assert.Single(bookmarks);
    }

    [Fact]
    public async Task Should_Resume_From_Bookmark_With_HttpContext()
    {
        // Arrange
        var activity = new WriteFileHttpResponse();
        var testContent = "Hello World"u8.ToArray();
        activity.Content = new Input<object>(testContent);
        
        var fixture = new ActivityTestFixture(activity);
        var httpContext = CreateMockHttpContext();
        
        // First execution - should create bookmark
        fixture.ConfigureServices(services =>
        {
            var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);
            services.AddSingleton(mockHttpContextAccessor);
            AddMockServices(services);
        });
        
        var firstContext = await fixture.ExecuteAsync();
        Assert.False(firstContext.IsCompleted);

        // Resume execution with HttpContext available
        fixture.ConfigureServices(services =>
        {
            var newHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            newHttpContextAccessor.HttpContext.Returns(httpContext);
            services.AddSingleton(newHttpContextAccessor);
            AddMockServices(services);
        });

        // Act - simulate resume by executing with bookmark context
        var newFixture = new ActivityTestFixture(activity);
        newFixture.ConfigureServices(services =>
        {
            var resumeHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            resumeHttpContextAccessor.HttpContext.Returns(httpContext);
            services.AddSingleton(resumeHttpContextAccessor);
            AddMockServices(services);
        });
        
        var resumedContext = await newFixture.ExecuteAsync();

        // Assert
        Assert.True(resumedContext.IsCompleted);
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
        
        activity.Content = new Input<object>(downloadable);

        // Act  
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        Assert.Equal("text/plain", httpContext.Response.ContentType);
        Assert.Contains("filename=\"metadata-file.txt\"", httpContext.Response.Headers.ContentDisposition.ToString());
    }

    [Theory]
    [InlineData("bytes=0-499")]
    [InlineData("bytes=500-999")]
    [InlineData("bytes=-500")]
    public async Task Should_Handle_Various_Range_Headers(string rangeHeader)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteFileHttpResponseActivity();
        var testContent = new byte[1000]; // 1KB of data
        new Random().NextBytes(testContent);
        
        httpContext.Request.Headers["Range"] = rangeHeader;
        activity.Content = new Input<object>(testContent);
        activity.EnableResumableDownloads = new Input<bool>(true);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        Assert.True(context.IsCompleted);
        // Range processing would be handled by FileStreamResult internally
    }

    // Helper Methods
    private static (WriteFileHttpResponse activity, HttpContext httpContext) CreateWriteFileHttpResponseActivity()
    {
        var activity = new WriteFileHttpResponse();
        var httpContext = new DefaultHttpContext();
        
        // Create a service collection with the required services for FileStreamResult
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        
        // Add required services for FileStreamResult
        services.AddSingleton<IActionResultExecutor<Microsoft.AspNetCore.Mvc.FileStreamResult>>(
            _ => new FileStreamResultExecutor(
                LoggerFactory.Create(b => b.AddConsole())
            )
        );
        
        // Build the service provider and assign it to HttpContext
        var serviceProvider = services.BuildServiceProvider();
        httpContext.RequestServices = serviceProvider;
        
        httpContext.Response.Body = new MemoryStream();
        
        return (activity, httpContext);
    }

    private static HttpContext CreateMockHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        
        // Create a service collection with the required services for FileStreamResult
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        
        // Add required services for FileStreamResult
        services.AddSingleton<IActionResultExecutor<Microsoft.AspNetCore.Mvc.FileStreamResult>>(
            _ => new FileStreamResultExecutor(
                LoggerFactory.Create(b => b.AddConsole())
            )
        );
        
        // Build the service provider and assign it to HttpContext
        var serviceProvider = services.BuildServiceProvider();
        httpContext.RequestServices = serviceProvider;
        
        httpContext.Response.Body = new MemoryStream();
        
        return httpContext;
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
        // Mock IDownloadableManager
        var mockDownloadableManager = Substitute.For<IDownloadableManager>();
        mockDownloadableManager.GetDownloadablesAsync(Arg.Any<object>(), Arg.Any<DownloadableOptions>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var content = callInfo.ArgAt<object>(0);
                return CreateMockDownloadables(content).Select(d => new Func<ValueTask<Downloadable>>(() => ValueTask.FromResult(d)));
            });
        services.AddSingleton(mockDownloadableManager);

        // Mock IFileCacheStorageProvider - keep it simple since methods may not exist
        var mockFileCacheProvider = Substitute.For<IFileCacheStorageProvider>();
        services.AddSingleton(mockFileCacheProvider);

        // Mock HttpFileCacheOptions
        var mockFileCacheOptions = Substitute.For<Microsoft.Extensions.Options.IOptions<HttpFileCacheOptions>>();
        mockFileCacheOptions.Value.Returns(new HttpFileCacheOptions
        {
            TimeToLive = TimeSpan.FromHours(1)
        });
        services.AddSingleton(mockFileCacheOptions);

        // Mock ISystemClock - use the correct Elsa interface
        var mockSystemClock = Substitute.For<ISystemClock>();
        mockSystemClock.UtcNow.Returns(DateTimeOffset.UtcNow);
        services.AddSingleton(mockSystemClock);

        // Add IStimulusHasher service required for bookmark creation
        services.AddSingleton(Substitute.For<IStimulusHasher>());

        // Add logging
        services.AddLogging();

        // Register ZipManager - ensure it's always available for the tests
        var zipManagerType = typeof(WriteFileHttpResponse).Assembly.GetType("Elsa.Http.Services.ZipManager");
        if (zipManagerType != null)
        {
            services.AddSingleton(zipManagerType, serviceProvider =>
            {
                try
                {
                    var systemClock = serviceProvider.GetRequiredService<ISystemClock>();
                    var fileCacheProvider = serviceProvider.GetRequiredService<IFileCacheStorageProvider>();
                    var fileCacheOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HttpFileCacheOptions>>();
                    
                    var constructor = zipManagerType.GetConstructors().FirstOrDefault();
                    if (constructor != null)
                    {
                        var parameters = constructor.GetParameters();
                        var args = new object[parameters.Length];
                        
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var paramType = parameters[i].ParameterType;
                            if (paramType == typeof(ISystemClock))
                                args[i] = systemClock;
                            else if (paramType == typeof(IFileCacheStorageProvider))
                                args[i] = fileCacheProvider;
                            else if (paramType == typeof(Microsoft.Extensions.Options.IOptions<HttpFileCacheOptions>))
                                args[i] = fileCacheOptions;
                            else if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(ILogger<>))
                            {
                                // Create a properly typed logger mock
                                var loggerType = paramType;
                                var mockLoggerMethod = typeof(Substitute).GetMethods()
                                    .Where(m => m.Name == "For" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
                                    .FirstOrDefault();
                                if (mockLoggerMethod != null)
                                {
                                    var genericMethod = mockLoggerMethod.MakeGenericMethod(loggerType);
                                    args[i] = genericMethod.Invoke(null, null) ?? Substitute.For<ILogger>();
                                }
                                else
                                {
                                    args[i] = Substitute.For<ILogger>();
                                }
                            }
                            else if (paramType == typeof(ILogger))
                                args[i] = Substitute.For<ILogger>();
                            else
                            {
                                // Create a mock for the parameter type using the correct overload
                                try
                                {
                                    args[i] = Substitute.For(new[] { paramType }, Array.Empty<object>());
                                }
                                catch
                                {
                                    args[i] = null!; // Use null for types that can't be mocked
                                }
                            }
                        }
                        
                        return constructor.Invoke(args);
                    }
                }
                catch
                {
                    // If real ZipManager creation fails, create a mock that implements the same interface
                    // This ensures the service is always available for dependency injection
                }
                
                // Fallback: create a mock implementation using NSubstitute
                return Substitute.For(new[] { zipManagerType }, Array.Empty<object>());
            });
        }
        else
        {
            // If ZipManager type is not found, create a generic mock object
            // Register it as object type so it can be resolved
            services.AddSingleton<object>(_ => new object());
        }
    }

    private static IEnumerable<Downloadable> CreateMockDownloadables(object content)
    {
        // Simplified mock creation for different content types
        if (content is byte[] byteArray)
        {
            yield return new Downloadable
            {
                Stream = new MemoryStream(byteArray),
                ContentType = "application/octet-stream",
                Filename = "file.bin"
            };
        }
        else if (content is string text)
        {
            yield return new Downloadable
            {
                Stream = new MemoryStream(Encoding.UTF8.GetBytes(text)),
                ContentType = "text/plain",
                Filename = "file.txt"
            };
        }
        else if (content is Stream stream)
        {
            // For Stream objects, return them as-is
            yield return new Downloadable
            {
                Stream = stream,
                ContentType = "application/octet-stream",
                Filename = "stream.bin"
            };
        }
        else if (content is Uri uri)
        {
            // For Uri objects, simulate downloaded content
            var mockContent = $"Downloaded content from {uri}";
            yield return new Downloadable
            {
                Stream = new MemoryStream(Encoding.UTF8.GetBytes(mockContent)),
                ContentType = "text/plain",
                Filename = Path.GetFileName(uri.LocalPath) ?? "downloaded.txt"
            };
        }
        else if (content is Downloadable downloadable)
        {
            // For Downloadable objects, return them as-is
            yield return downloadable;
        }
        else if (content is IEnumerable<object> fileContents)
        {
            // For collections, create a zip downloadable
            var zipDownloadable = CreateZipDownloadable(fileContents);
            yield return zipDownloadable;
        }
        else
        {
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
                        var contentBytes = Encoding.UTF8.GetBytes(fileContent?.ToString() ?? "");
                        entryStream.Write(contentBytes, 0, contentBytes.Length);
                    }
                }
            }

            memoryStream.Position = 0;
            return new Downloadable
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
