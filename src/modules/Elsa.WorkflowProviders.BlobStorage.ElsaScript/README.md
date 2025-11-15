# Elsa.WorkflowProviders.BlobStorage.ElsaScript

This module provides ElsaScript support for the BlobStorage workflows provider, enabling workflows stored as `.elsa` files to be loaded from blob storage.

## Overview

This is an optional integration module that bridges:
- `Elsa.WorkflowProviders.BlobStorage` - Loads workflows from blob storage
- `Elsa.Scripting.ElsaScript` - Compiles ElsaScript DSL to Workflow objects

## Installation

Reference the project in your application:

```xml
<ProjectReference Include="path/to/Elsa.WorkflowProviders.BlobStorage.ElsaScript.csproj" />
```

## Usage

Enable the feature when configuring your Elsa application:

```csharp
services.AddElsa(elsa => elsa
    .UseBlobStorage(blob => blob
        // Configure blob storage...
    )
    .UseElsaScriptBlobStorage() // Enable .elsa file support
);
```

## How It Works

The module registers an `ElsaScriptBlobWorkflowFormatHandler` that:

1. Recognizes `.elsa` files in blob storage
2. Reads the ElsaScript content
3. Compiles it using `ElsaScriptCompiler`
4. Returns a `MaterializedWorkflow` ready for execution

## Example

Create a workflow file `hello.elsa` in your blob storage:

```elsa
for i = 0 to 3 {
  WriteLine(Text: "Hello from ElsaScript!");
}
```

The file will be automatically discovered and loaded by the BlobStorage provider.

## Architecture

This follows the **format handler pattern** introduced in `Elsa.WorkflowProviders.BlobStorage`:

- **No tight coupling**: BlobStorage doesn't reference ElsaScript
- **Optional integration**: ElsaScript remains opt-in
- **Extensible**: Other DSL formats can be added the same way

## Dependencies

- `Elsa.WorkflowProviders.BlobStorage` - The base blob storage provider
- `Elsa.Scripting.ElsaScript` - The ElsaScript compiler

## See Also

- [ElsaScript Documentation](../Elsa.Scripting.ElsaScript/README.md)
- [BlobStorage Provider](../Elsa.WorkflowProviders.BlobStorage/README.md)
