using System.IO.Compression;
using System.Text;
using Elsa.Compression.Activities;
using Elsa.Compression.Models;
using Elsa.Compression.Services;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Elsa.Compression.UnitTests.Activities;

public class CreateZipArchiveTests
{
    [Fact]
    public async Task CreateZipArchive_WithByteArrayEntry_CreatesValidZip()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IZipEntryContentResolver, ZipEntryContentResolver>();
        var serviceProvider = services.BuildServiceProvider();

        var activity = new CreateZipArchive("test.zip", new List<object>());
        var context = CreateActivityExecutionContext(serviceProvider);
        
        var testContent = "Hello, World!"u8.ToArray();
        context.Set(activity.Entries, new object[] { testContent });

        // Act
        await activity.ExecuteAsync(context);

        // Assert
        var result = context.Get(activity.Result);
        Assert.NotNull(result);
        
        result.Position = 0;
        using var zipArchive = new ZipArchive(result, ZipArchiveMode.Read);
        Assert.Single(zipArchive.Entries);
        
        var entry = zipArchive.Entries.First();
        Assert.Equal("file.bin", entry.Name);
        
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        var content = await reader.ReadToEndAsync();
        Assert.Equal("Hello, World!", content);
    }

    [Fact]
    public async Task CreateZipArchive_WithZipEntry_UsesCorrectEntryName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IZipEntryContentResolver, ZipEntryContentResolver>();
        var serviceProvider = services.BuildServiceProvider();

        var activity = new CreateZipArchive("test.zip", new List<object>());
        var context = CreateActivityExecutionContext(serviceProvider);
        
        var zipEntry = new ZipEntry("Test content", "custom.txt");
        context.Set(activity.Entries, new object[] { zipEntry });

        // Act
        await activity.ExecuteAsync(context);

        // Assert
        var result = context.Get(activity.Result);
        Assert.NotNull(result);
        
        result.Position = 0;
        using var zipArchive = new ZipArchive(result, ZipArchiveMode.Read);
        Assert.Single(zipArchive.Entries);
        
        var entry = zipArchive.Entries.First();
        Assert.Equal("custom.txt", entry.Name);
    }

    [Fact]
    public async Task CreateZipArchive_WithBase64Entry_DecodesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IZipEntryContentResolver, ZipEntryContentResolver>();
        var serviceProvider = services.BuildServiceProvider();

        var activity = new CreateZipArchive("test.zip", new List<object>());
        var context = CreateActivityExecutionContext(serviceProvider);
        
        var originalText = "Base64 test content";
        var base64Content = "base64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));
        context.Set(activity.Entries, new object[] { base64Content });

        // Act
        await activity.ExecuteAsync(context);

        // Assert
        var result = context.Get(activity.Result);
        Assert.NotNull(result);
        
        result.Position = 0;
        using var zipArchive = new ZipArchive(result, ZipArchiveMode.Read);
        Assert.Single(zipArchive.Entries);
        
        var entry = zipArchive.Entries.First();
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        var content = await reader.ReadToEndAsync();
        Assert.Equal(originalText, content);
    }

    private static ActivityExecutionContext CreateActivityExecutionContext(IServiceProvider serviceProvider)
    {
        var workflowExecutionContext = Substitute.For<WorkflowExecutionContext>();
        workflowExecutionContext.ServiceProvider.Returns(serviceProvider);
        workflowExecutionContext.CancellationToken.Returns(CancellationToken.None);

        var activity = Substitute.For<IActivity>();
        var activityExecutionContext = new ActivityExecutionContext(
            workflowExecutionContext,
            activity,
            default,
            default,
            default,
            CancellationToken.None);

        return activityExecutionContext;
    }
}