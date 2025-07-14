# Elsa Compression Activities

This directory contains a demonstration of the Elsa Compression module that is already fully implemented in the repository.

## Overview

The `Elsa.IO.Compression` module provides compression and archiving activities for Elsa Workflows. The main feature is the `CreateZipArchive` activity that allows creating ZIP archives from various input formats.

## Features

### CreateZipArchive Activity

The `CreateZipArchive` activity supports all the requirements specified in the issue:

- **Input Formats**: Supports multiple input formats including:
  - `byte[]` - Byte arrays
  - `Stream` - Stream objects
  - `string` - File paths and URLs
  - `string` - Base64 strings (with `base64:` prefix)
  - `ZipEntry` - Custom objects with content and entry names
  - Arrays of any of the above types

- **Output**: Returns a `Stream` that contains the ZIP archive

- **Configuration**: Configurable compression levels (Optimal, Fastest, SmallestSize, NoCompression)

- **Error Handling**: Proper error handling with logging support

## Usage Examples

### Basic Usage with Byte Arrays

```csharp
var activity = new CreateZipArchive();
var entries = new object[] {
    Encoding.UTF8.GetBytes("Content 1"),
    Encoding.UTF8.GetBytes("Content 2")
};

activity.Entries.Set(context, entries);
await activity.ExecuteAsync(context);
var result = activity.Result.Get(context);
```

### Using ZipEntry Objects with Custom Names

```csharp
var entries = new object[] {
    new ZipEntry(Encoding.UTF8.GetBytes("Document content"), "document.txt"),
    new ZipEntry(Encoding.UTF8.GetBytes("Config data"), "config.json")
};

activity.Entries.Set(context, entries);
```

### Using Base64 Strings

```csharp
var entries = new object[] {
    "base64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes("Content 1")),
    "base64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes("Content 2"))
};

activity.Entries.Set(context, entries);
```

### Mixed Entry Types

```csharp
var entries = new object[] {
    Encoding.UTF8.GetBytes("Byte array content"),
    new MemoryStream(Encoding.UTF8.GetBytes("Stream content")),
    new ZipEntry(Encoding.UTF8.GetBytes("Custom content"), "custom.txt"),
    "base64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes("Base64 content"))
};

activity.Entries.Set(context, entries);
```

### Setting Compression Level

```csharp
activity.CompressionLevel.Set(context, CompressionLevel.Fastest);
```

## Module Structure

```
src/modules/Elsa.IO.Compression/
├── Activities/
│   └── CreateZipArchive.cs         # Main activity implementation
├── Models/
│   └── ZipEntry.cs                 # Custom ZipEntry model
├── Services/Strategies/
│   └── ZipEntryContentStrategy.cs  # Content resolution strategy
├── Features/
│   └── CompressionFeature.cs       # Module feature configuration
└── Extensions/
    └── ModuleExtensions.cs         # Module extensions
```

## Integration

To use the compression module in your workflows:

1. Add the module to your service configuration:
```csharp
services.AddElsa(elsa =>
{
    elsa.UseWorkflows()
        .UseIO()
        .UseCompression();
});
```

2. Use the `CreateZipArchive` activity in your workflows through the designer or programmatically.

## Implementation Details

- The activity inherits from `CodeActivity<Stream>` and returns a Stream result
- Content resolution is handled by the `IContentResolver` service
- The `ZipEntryContentStrategy` provides specialized handling for `ZipEntry` objects
- Proper error handling with warning logs for individual entry failures
- Memory-efficient stream processing

## Status

✅ **COMPLETE** - All requirements from the original issue have been implemented:
- Creates ZIP archives from collections of entries
- Supports all specified input formats
- Returns Stream output
- Handles arrays of entries
- Includes proper error handling and logging

## Future Enhancements

The module can be extended with additional activities:
- Extract archive activity
- Update archive activity
- List archive entries activity
- Remove entries activity
- Additional compression formats (gzip, tar, etc.)

## Demo

Run the demo script to see the module structure and usage examples:

```bash
./demo.sh
```

This demonstrates that the Elsa Compression Activities are fully implemented and ready for use in workflows.