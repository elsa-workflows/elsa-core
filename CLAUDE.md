# CLAUDE.md - Elsa Workflows

This file provides guidance for Claude Code when working with this repository.

## Project Overview

**Elsa Workflows** is a .NET workflow library enabling workflow execution within any .NET application. Version 3.x supports .NET 8.0, 9.0, and 10.0 with both visual designer and programmatic workflow definition capabilities.

- **Language**: C# (.NET 10.0 primary)
- **Architecture**: Modular library with 100+ projects
- **License**: MIT
- **Build System**: NUKE build automation

## Repository Structure

```
src/
├── apps/           # Reference applications (Elsa.Server.Web, etc.)
├── common/         # Shared libraries
├── modules/        # Core functionality modules (70+ projects)
│   ├── Elsa.Workflows.Core     # Core workflow engine
│   ├── Elsa.Workflows.Runtime  # Runtime execution
│   ├── Elsa.Workflows.Api      # REST API
│   └── ...
├── extensions/     # Extension projects
└── clients/        # API clients
test/
├── unit/           # Unit tests
├── integration/    # Integration tests
├── component/      # Component tests
└── performance/    # Performance tests
build/              # NUKE build configuration
docker/             # Docker configurations
```

## Build Commands

### Primary Build (NUKE)

```bash
# macOS/Linux
./build.sh Compile              # Compile all
./build.sh Test                 # Run tests
./build.sh Pack                 # Create NuGet packages
./build.sh Compile Test Pack    # Full CI pipeline

# Windows
.\build.cmd Compile Test Pack
```

### Direct dotnet Commands

```bash
# Always use --ignore-failed-sources for restore
dotnet restore --ignore-failed-sources
dotnet build --no-restore

# Build specific module
dotnet build src/modules/Elsa.Workflows.Core/

# Run tests for specific project
dotnet test test/unit/Elsa.Workflows.Core.UnitTests/
```

## Key Guidelines

### Package Restore

**Always use `--ignore-failed-sources`** when restoring packages. Expected warnings:
- `NU1900`: Unable to load service index for external feeds (safe to ignore)
- `NU1801`: Service index warnings for feedz.io (safe to ignore)

### Code Standards

- C# latest language version
- Target framework: .NET 10.0
- Nullable reference types: Enabled
- Implicit usings: Enabled
- 4-space indentation

### Architecture Patterns

- **Modular design**: Each feature is a separate project
- **Dependency injection**: Microsoft.Extensions.DependencyInjection throughout
- **Activity-based**: Workflows built from composable activities
- **Async/await**: Used extensively for scalability

### Core Modules (Most Reliable)

Start with these modules when building/testing - they have fewer external dependencies:
- `Elsa.Workflows.Core`
- `Elsa.Workflows.Runtime`
- `Elsa.Workflows.Management`

## Running the Server

```bash
cd src/apps/Elsa.Server.Web
dotnet restore --ignore-failed-sources
dotnet run
```

## Testing

```bash
# Run all tests via NUKE
./build.sh Test

# Run specific test project
dotnet test test/unit/Elsa.Workflows.Core.UnitTests/

# Run with no build (if already built)
dotnet test --no-build test/unit/Elsa.Workflows.Core.UnitTests/
```

## Configuration Files

- `build/Build.cs` - NUKE build configuration
- `Directory.Packages.props` - Central package management
- `Directory.Build.props` - Global MSBuild settings
- `NuGet.Config` - Package sources and mapping
- `Elsa.sln` - Solution file (119 projects)

## Common Issues

1. **External package errors** (`NU1101` for Elsa.Studio packages): Focus on core modules instead
2. **WebhooksCore dependency failures**: Use `--ignore-failed-sources` consistently
3. **Slow initial build**: Normal for 100+ projects (5-10 minutes); incremental builds are faster

## Important Notes

- Build warnings (NU1900/NU1801) are expected - don't try to fix them
- Some projects have conditional framework targeting
- Studio components come from a separate repository (elsa-studio)
- Docker images available for development: `elsaworkflows/elsa-server-and-studio-v3`

