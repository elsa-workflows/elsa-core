using System.IO.Compression;
using System.Text;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.IO.Compression.Activities;
using Elsa.IO.Compression.Features;
using Elsa.IO.Compression.Models;
using Elsa.IO.Features;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.IO.Compression.Demo;

/// <summary>
/// Demonstrates the usage of the CreateZipArchive activity with different input types.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Elsa Compression Activities Demo ===");
        Console.WriteLine();
        
        // Setup services
        var services = new ServiceCollection();
        services.AddElsa(elsa =>
        {
            elsa.UseWorkflows()
                .UseIO()
                .UseCompression();
        });
        
        services.AddLogging(builder => builder.AddConsole());
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Demo 1: Create zip with byte array entries
        Console.WriteLine("Demo 1: Creating zip with byte array entries");
        await DemoByteArrayEntries(serviceProvider);
        Console.WriteLine();
        
        // Demo 2: Create zip with ZipEntry objects
        Console.WriteLine("Demo 2: Creating zip with ZipEntry objects");
        await DemoZipEntryObjects(serviceProvider);
        Console.WriteLine();
        
        // Demo 3: Create zip with base64 entries
        Console.WriteLine("Demo 3: Creating zip with base64 entries");
        await DemoBase64Entries(serviceProvider);
        Console.WriteLine();
        
        // Demo 4: Create zip with mixed entry types
        Console.WriteLine("Demo 4: Creating zip with mixed entry types");
        await DemoMixedEntries(serviceProvider);
        Console.WriteLine();
        
        Console.WriteLine("All demos completed successfully!");
    }
    
    private static async Task DemoByteArrayEntries(IServiceProvider services)
    {
        var activity = new CreateZipArchive();
        var context = await CreateActivityExecutionContext(services);
        
        // Create some byte array entries
        var entry1 = Encoding.UTF8.GetBytes("This is the content of file 1");
        var entry2 = Encoding.UTF8.GetBytes("This is the content of file 2");
        var entries = new object[] { entry1, entry2 };
        
        // Set the entries input
        activity.Entries.Set(context, entries);
        
        // Execute the activity
        await activity.ExecuteAsync(context);
        
        // Get the result
        var result = activity.Result.Get(context);
        
        Console.WriteLine($"Created zip archive with {result.Length} bytes");
        
        // Verify the zip by reading its contents
        using var zipArchive = new ZipArchive(result, ZipArchiveMode.Read);
        Console.WriteLine($"Archive contains {zipArchive.Entries.Count} entries:");
        
        foreach (var entry in zipArchive.Entries)
        {
            Console.WriteLine($"  - {entry.Name} ({entry.Length} bytes)");
        }
    }
    
    private static async Task DemoZipEntryObjects(IServiceProvider services)
    {
        var activity = new CreateZipArchive();
        var context = await CreateActivityExecutionContext(services);
        
        // Create ZipEntry objects with custom names
        var entry1 = new ZipEntry(Encoding.UTF8.GetBytes("Hello from document 1"), "document1.txt");
        var entry2 = new ZipEntry(Encoding.UTF8.GetBytes("Hello from document 2"), "document2.txt");
        var entry3 = new ZipEntry(Encoding.UTF8.GetBytes("Configuration data"), "config.json");
        var entries = new object[] { entry1, entry2, entry3 };
        
        // Set the entries input
        activity.Entries.Set(context, entries);
        
        // Execute the activity
        await activity.ExecuteAsync(context);
        
        // Get the result
        var result = activity.Result.Get(context);
        
        Console.WriteLine($"Created zip archive with {result.Length} bytes");
        
        // Verify the zip by reading its contents
        using var zipArchive = new ZipArchive(result, ZipArchiveMode.Read);
        Console.WriteLine($"Archive contains {zipArchive.Entries.Count} entries:");
        
        foreach (var entry in zipArchive.Entries)
        {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            var content = await reader.ReadToEndAsync();
            Console.WriteLine($"  - {entry.Name}: \"{content}\"");
        }
    }
    
    private static async Task DemoBase64Entries(IServiceProvider services)
    {
        var activity = new CreateZipArchive();
        var context = await CreateActivityExecutionContext(services);
        
        // Create base64 entries
        var content1 = "This is base64 encoded content 1";
        var content2 = "This is base64 encoded content 2";
        var base64Entry1 = "base64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(content1));
        var base64Entry2 = "base64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(content2));
        var entries = new object[] { base64Entry1, base64Entry2 };
        
        // Set the entries input
        activity.Entries.Set(context, entries);
        
        // Execute the activity
        await activity.ExecuteAsync(context);
        
        // Get the result
        var result = activity.Result.Get(context);
        
        Console.WriteLine($"Created zip archive with {result.Length} bytes");
        
        // Verify the zip by reading its contents
        using var zipArchive = new ZipArchive(result, ZipArchiveMode.Read);
        Console.WriteLine($"Archive contains {zipArchive.Entries.Count} entries:");
        
        foreach (var entry in zipArchive.Entries)
        {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            var content = await reader.ReadToEndAsync();
            Console.WriteLine($"  - {entry.Name}: \"{content}\"");
        }
    }
    
    private static async Task DemoMixedEntries(IServiceProvider services)
    {
        var activity = new CreateZipArchive();
        var context = await CreateActivityExecutionContext(services);
        
        // Create mixed entry types
        var byteEntry = Encoding.UTF8.GetBytes("Byte array content");
        var streamEntry = new MemoryStream(Encoding.UTF8.GetBytes("Stream content"));
        var zipEntry = new ZipEntry(Encoding.UTF8.GetBytes("ZipEntry content"), "custom.txt");
        var base64Entry = "base64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes("Base64 content"));
        
        var entries = new object[] { byteEntry, streamEntry, zipEntry, base64Entry };
        
        // Set the entries input
        activity.Entries.Set(context, entries);
        
        // Set compression level
        activity.CompressionLevel.Set(context, CompressionLevel.Fastest);
        
        // Execute the activity
        await activity.ExecuteAsync(context);
        
        // Get the result
        var result = activity.Result.Get(context);
        
        Console.WriteLine($"Created zip archive with {result.Length} bytes");
        
        // Verify the zip by reading its contents
        using var zipArchive = new ZipArchive(result, ZipArchiveMode.Read);
        Console.WriteLine($"Archive contains {zipArchive.Entries.Count} entries:");
        
        foreach (var entry in zipArchive.Entries)
        {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            var content = await reader.ReadToEndAsync();
            Console.WriteLine($"  - {entry.Name}: \"{content}\"");
        }
    }
    
    private static async Task<ActivityExecutionContext> CreateActivityExecutionContext(IServiceProvider services)
    {
        var workflowExecutionContext = new WorkflowExecutionContext
        {
            ServiceProvider = services,
            Id = Guid.NewGuid().ToString(),
            DefinitionId = "demo-workflow",
            DefinitionVersionId = Guid.NewGuid().ToString(),
            CancellationToken = CancellationToken.None
        };
        
        var activityDescriptor = new ActivityDescriptor
        {
            Id = Guid.NewGuid().ToString(),
            Type = nameof(CreateZipArchive)
        };
        
        var activity = new CreateZipArchive();
        
        var context = new ActivityExecutionContext(
            id: Guid.NewGuid().ToString(),
            workflowExecutionContext: workflowExecutionContext,
            parentActivityExecutionContext: null,
            activity: activity,
            activityDescriptor: activityDescriptor,
            createdAt: DateTimeOffset.UtcNow,
            properties: new Dictionary<string, object>(),
            systemClock: services.GetRequiredService<ISystemClock>(),
            cancellationToken: CancellationToken.None
        );
        
        return context;
    }
}