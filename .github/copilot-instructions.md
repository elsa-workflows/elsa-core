# Copilot Coding Agent Instructions for Elsa Workflows

## Repository Overview

**Elsa Workflows** is a powerful .NET workflow library that enables workflow execution within any .NET application. This is version 3.0, supporting .NET 9.0 and providing both a visual designer and programmatic workflow definition capabilities.

### Key Statistics
- **Language**: C# (.NET 9.0)
- **Architecture**: Modular library with 104+ projects
- **Code Size**: ~3,500 C# files across modules
- **License**: MIT
- **Build System**: NUKE build automation
- **Target Frameworks**: .NET 9.0 (primary)

## High-Level Architecture

### Directory Structure
```
src/
├── apps/               # Reference applications (5 projects)
│   ├── Elsa.Server.Web            # Workflow server only
│   ├── Elsa.ServerAndStudio.Web    # Combined server + studio  
│   ├── Elsa.Studio.Web             # Studio web interface
│   ├── ElsaStudioWebAssembly       # Studio WebAssembly app
│   └── Elsa.Server.LoadBalancer    # Load balancer
├── common/             # Shared libraries (8 projects)
├── modules/            # Core functionality modules (70+ projects)
│   ├── Elsa.Workflows.Core         # Core workflow engine
│   ├── Elsa.Workflows.Runtime      # Runtime execution
│   ├── Elsa.Workflows.Api          # REST API
│   ├── Elsa.Http                   # HTTP activities
│   ├── Elsa.Email                  # Email activities
│   └── [many others]               # Database, messaging, etc.
└── clients/            # API clients
test/
├── unit/               # Unit tests
├── integration/        # Integration tests  
├── component/          # Component tests
└── performance/        # Performance tests
build/                  # NUKE build configuration
docker/                 # Docker configurations
```

### Core Components
- **Elsa.Workflows.Core**: Main workflow engine and activities
- **Elsa.Workflows.Runtime**: Workflow execution runtime
- **Elsa.Workflows.Api**: RESTful API for workflow management
- **Elsa.Workflows.Management**: Workflow definition management
- **Elsa modules**: Specialized functionality (HTTP, email, scheduling, etc.)

## Build Instructions

### Prerequisites
- **.NET 9.0 SDK** (verified working version: 9.0.305)
- **Build time**: Initial restore ~1-2 minutes, full compile ~5-10 minutes

### Critical Build Information

⚠️ **IMPORTANT**: The repository has external dependencies that may cause build failures:

1. **External NuGet Feeds**: Some projects depend on packages from:
   - `https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json` (Elsa Studio packages)
   - `https://f.feedz.io/sfmskywalker/webhooks-core/nuget/index.json` (Webhooks packages)

2. **Build Failure Workarounds**:
   - Studio apps (`Elsa.Studio.Web`, `ElsaStudioWebAssembly`, `Elsa.ServerAndStudio.Web`) depend on prebuilt studio packages that may not be accessible
   - Server app (`Elsa.Server.Web`) depends on WebhooksCore package that may not be accessible  
   - Core workflow functionality can be built independently
   - Some test projects may fail due to missing external packages

### Build Commands

**Primary build script**: `./build.sh` (Linux/macOS) or `.\build.cmd` (Windows)

```bash
# View available targets
./build.sh --help

# Clean build artifacts
./build.sh Clean

# Restore packages (may show warnings for inaccessible feeds)
./build.sh Restore --ignore-failed-sources

# Compile core components (excludes studio apps)
./build.sh Compile

# Run tests (limited due to external dependencies) 
./build.sh Test

# Create NuGet packages
./build.sh Pack

# Full CI pipeline (compile, test, pack)
./build.sh Compile Test Pack
```

**Direct dotnet commands** for core components:
```bash
# Build specific core projects that don't require external packages
dotnet restore src/modules/Elsa.Workflows.Core/ --ignore-failed-sources
dotnet build src/modules/Elsa.Workflows.Core/ --no-restore
dotnet restore src/modules/Elsa.Workflows.Runtime/ --ignore-failed-sources  
dotnet build src/modules/Elsa.Workflows.Runtime/ --no-restore

# Note: Server apps may fail due to WebhooksCore dependency
# Build and test individual core modules
find test/unit -name "*.csproj" | head -5 | xargs -I {} dotnet build {}
```

### Expected Build Warnings
- `NU1900`: Unable to load service index for external feeds (safe to ignore)
- `NU1801`: Service index warnings for feedz.io sources (safe to ignore)
- `NU1101`: Missing Elsa.Studio packages (blocks studio app builds)

### Successful Build Indicators
- Core modules (Elsa.Workflows.Core, etc.) compile successfully
- Server applications (Elsa.Server.Web) build without the studio UI
- Most modules show "succeeded with X warning(s)" (warnings are acceptable)

## Testing

### Test Structure
- **Unit tests**: `test/unit/` - Fast, isolated tests
- **Integration tests**: `test/integration/` - End-to-end scenarios
- **Component tests**: `test/component/` - Feature testing
- **Performance tests**: `test/performance/` - Benchmarks

### Running Tests
```bash
# Via NUKE build system
./build.sh Test

# Direct dotnet test (for accessible projects)
dotnet test test/unit/[specific-project]/
dotnet test --no-build --no-restore [project-path]
```

**Note**: Many tests may fail to run due to external package dependencies. Focus on core workflow engine tests that don't require studio packages.

## Development Guidelines

### Code Standards
- **Language version**: C# latest
- **Target framework**: .NET 9.0  
- **Nullable reference types**: Enabled
- **Implicit usings**: Enabled
- **EditorConfig**: Configured (4-space indentation, CRLF line endings)

### Architecture Patterns
- **Modular design**: Each feature area is a separate project/module
- **Dependency injection**: Heavy use of Microsoft.Extensions.DependencyInjection
- **Activity-based**: Workflows are built from composable activities
- **Async/await**: Extensive use throughout for scalability

### Common Gotchas
1. **External Dependencies**: Studio-related projects require external packages
2. **NuGet Source Mapping**: Configured in NuGet.Config, restricts where packages can be sourced
3. **Multiple Target Frameworks**: Some projects conditionally target different frameworks
4. **Build Warnings**: Many NU1900/NU1801 warnings are expected and safe

## Continuous Integration

### GitHub Actions Workflow
- **Trigger**: Pull requests to `main` branch
- **Runner**: ubuntu-latest  
- **.NET Version**: 9.x (latest)
- **Commands**: `./build.cmd Compile Test Pack`
- **File**: `.github/workflows/pr.yml` (auto-generated by NUKE)

### CI Pipeline Steps
1. Checkout code
2. Setup .NET 9.x SDK
3. Execute: Compile → Test → Pack
4. Expected warnings for external feed access
5. Studio apps may be excluded from CI builds

## Key Configuration Files

- **Build**: `build/Build.cs` (NUKE build configuration)
- **Dependencies**: `Directory.Packages.props` (central package management)
- **Global settings**: `Directory.Build.props`
- **NuGet**: `NuGet.Config` (package sources and mapping)
- **Solution**: `Elsa.sln` (119 projects)
- **Docker**: `docker/` directory with multiple Dockerfiles
- **GitHub Actions**: `.github/workflows/` (auto-generated)

## Docker Support

Multiple Docker configurations available:
- `ElsaServer.Dockerfile` - Server only
- `ElsaServerAndStudio.Dockerfile` - Combined server + studio
- `ElsaStudio.Dockerfile` - Studio only
- Docker Compose configurations for development

## Quick Start for Development

1. **Clone and build core components**:
   ```bash
   git clone [repo-url]
   cd elsa-core
   ./build.sh Clean Restore --ignore-failed-sources
   ```

2. **Work with core modules** (avoid studio dependencies):
   ```bash
   cd src/modules/Elsa.Workflows.Core
   dotnet build
   dotnet test ../../test/unit/[related-tests]/
   ```

3. **Work with core modules that don't require external dependencies**:
   ```bash
   cd src/modules/Elsa.Workflows.Core
   dotnet restore --ignore-failed-sources
   dotnet build --no-restore
   ```

## Running the Applications

### Development Workflow Server

To run the workflow server for development:

```bash
cd src/apps/Elsa.Server.Web
dotnet restore --ignore-failed-sources
dotnet run
```

The server will start on the configured ports (check `appsettings.json` or environment variables).

### Combined Server and Studio

To run both the workflow server and visual studio together:

```bash
cd src/apps/Elsa.ServerAndStudio.Web
dotnet restore --ignore-failed-sources
dotnet run
```

Access the studio at the configured URL (typically `http://localhost:5000` or as configured).

### Using Docker

For quick testing with Docker (see README for full details):

```bash
docker pull elsaworkflows/elsa-server-and-studio-v3:latest
docker run -t -i -e ASPNETCORE_ENVIRONMENT='Development' -e HTTP_PORTS=8080 -e HTTP__BASEURL=http://localhost:13000 -p 13000:8080 elsaworkflows/elsa-server-and-studio-v3:latest
```

Default credentials: username `admin`, password `password`

## Troubleshooting

### Common Issues

1. **Missing External Packages**: If you encounter `NU1101` errors for Elsa.Studio packages:
   - This is expected for studio-related apps
   - Focus development on core workflow modules instead
   - Or use Docker images that have pre-built studio components

2. **Build Fails on Server Apps**: If `Elsa.Server.Web` fails due to WebhooksCore:
   - This is a known issue with external package feeds
   - Try building individual core modules instead
   - Use `--ignore-failed-sources` flag consistently

3. **Test Failures**: If many tests fail to run:
   - External package dependencies may be unavailable
   - Run tests for specific core modules individually
   - Focus on tests that don't require studio packages

4. **Slow Initial Build**: First restore and compile can take 5-10+ minutes:
   - This is normal for a large solution with 100+ projects
   - Subsequent builds are much faster (incremental)
   - Consider building specific projects/modules when iterating

## Additional Resources

### Documentation
- **Official Documentation**: [https://docs.elsaworkflows.io/](https://docs.elsaworkflows.io/)
- **README**: See [README.md](../README.md) for quick start and features overview
- **Contributing Guide**: See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines

### Community Support
- **GitHub Issues**: [Report bugs and request features](https://github.com/elsa-workflows/elsa-core/issues)
- **GitHub Discussions**: [Ask questions and discuss](https://github.com/elsa-workflows/elsa-core/discussions)
- **Discord**: [Join the community chat](https://discord.gg/hhChk5H472)
- **Stack Overflow**: [Tag: elsa-workflows](http://stackoverflow.com/questions/tagged/elsa-workflows)

### Enterprise Support
- **ELSA-X**: [Professional support and enterprise solutions](https://elsa-x.io)

## Important Notes for Coding Agents

1. **Always use `--ignore-failed-sources`** when restoring packages
2. **Focus on core workflow functionality** rather than studio UI components  
3. **Studio apps require external packages** that may not be accessible
4. **Build warnings are normal** - don't try to fix NU1900/NU1801 warnings
5. **Test individual modules** rather than solution-wide tests when external deps fail
6. **Use direct dotnet commands** for building specific components when NUKE fails
7. **Check project references** before attempting builds - some projects have conditional references
8. **Start with core modules** like `Elsa.Workflows.Core`, `Elsa.Workflows.Runtime` which are more likely to build successfully

Trust these instructions for build and development workflows. Only search for additional information if these instructions are incomplete or found to be incorrect.