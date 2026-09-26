# Elsa Studio - Copilot Coding Agent Instructions

## Repository Overview

**Elsa Studio** is a modular Blazor application framework built with MudBlazor for managing Elsa Workflows.

- **Size**: ~250 projects (framework, modules, bundles, hosts)
- **Stack**: C# (.NET 8/9), Blazor (Server/WASM), TypeScript, React, MudBlazor
- **Build**: .NET SDK + npm/webpack for client assets

## Critical Build Requirements

**Prerequisites**: .NET 9 SDK (9.0.306+), .NET 8 SDK, Node.js 20.x+, npm 10.x+

**Version Management**: `Directory.Packages.props` uses Central Package Management. Two key properties:
- `ElsaVersion` - Controls Elsa.Api.Client packages from feedz.io preview feed
- `MicrosoftVersion` - Controls Microsoft.AspNetCore.* and Microsoft.Extensions.* packages

**Common Issues**:
- **NU1102 errors**: Preview Elsa packages unavailable. Update `ElsaVersion` to "Nearest version" from error message.
- **NU1605 warnings**: Microsoft package downgrade. Update `MicrosoftVersion` to match Elsa requirements (typically 9.0.9+).

## Build Process (Step-by-Step)

**ALWAYS follow this sequence**:

**1. Build NPM Assets** (REQUIRED before .NET build - takes ~25 seconds):
```bash
cd src/modules/Elsa.Studio.Workflows.Designer/ClientLib && npm install --force && npm run build && cd ../../../..
cd src/framework/Elsa.Studio.DomInterop/ClientLib && npm install --force && npm run build && cd ../../../..
```
Use `--force` to bypass peer dependency warnings (expected and safe).

**2. Build .NET** (takes ~90 seconds, expect 1500+ doc warnings but 0 errors):
```bash
dotnet restore Elsa.Studio.sln
dotnet build Elsa.Studio.sln --configuration Release
```

**3. Run Hosts**:
- Blazor Server: `dotnet run --project src/hosts/Elsa.Studio.Host.Server/Elsa.Studio.Host.Server.csproj`
- Blazor WASM: `dotnet run --project src/hosts/Elsa.Studio.Host.Wasm/Elsa.Studio.Host.Wasm.csproj`
- Backend URL configured in `appsettings.json` (default: `https://localhost:5001/elsa/api`)

**4. Testing**: No test projects exist. CI runs `dotnet test` but completes immediately.

## Project Structure

**Root Files**:
- `Elsa.Studio.sln` / `Elsa.Studio-Core.sln` - Full / Core-only solutions
- `Directory.Build.props` - MSBuild properties (targeting, versioning, nullable enabled)
- `Directory.Packages.props` - Central Package Management versions
- `NuGet.Config` - Package sources (nuget.org + feedz.io for Elsa previews)

**Source Organization** (`src/`):

**Framework** (`framework/`) - Core infrastructure:
- `Core` - Core abstractions/services; `Core.BlazorServer`/`Core.BlazorWasm` - Platform implementations
- `Shell` - App shell/navigation; `Shared` - Shared components; `Translations` - Localization
- `DomInterop` - DOM interop (has ClientLib with TypeScript/webpack)

**Modules** (`modules/`) - Features:
- `Workflows`, `Workflows.Core`, `Workflows.Designer` - Workflow management (Designer has ClientLib using @antv/x6)
- `Localization.*`, `Login.*` - Localization and auth with Blazor-specific variants
- `Dashboard`, `Environments`, `Labels`, `Security`, `UIHints`, etc.

**Bundles** (`bundles/`) - `Elsa.Studio` - Main bundle combining framework/modules

**Hosts** (`hosts/`) - Runnable apps:
- `Host.Server` - Blazor Server; `Host.Wasm` - Blazor WebAssembly
- `Host.HostedWasm` - Hosted WASM; `Host.CustomElements` - For npm embedding

**Wrappers** (`wrappers/`) - `react-wrapper` - React wrapper for WASM (uses Lerna monorepo)

**ClientLib Projects**: Two TypeScript/webpack projects that build to parent's `wwwroot/` (gitignored):
- `framework/Elsa.Studio.DomInterop/ClientLib/` - DOM utilities
- `modules/Elsa.Studio.Workflows.Designer/ClientLib/` - Workflow designer (@antv/x6)

## GitHub Actions CI/CD

**Workflow**: `.github/workflows/packages.yml` (ubuntu-latest, 30-min timeout)

**Process**: Sets version → Builds npm assets (`--force`) → Restores/builds/tests .NET → Packs NuGet → Publishes CustomElements → Packs npm (wasm + react) → Uploads artifacts → Publishes to feedz.io (preview) or nuget.org/npmjs.com (releases)

**Feeds**: Preview to feedz.io, stable releases to nuget.org/npmjs.org

## Configuration & Conventions

**Linting**: ReSharper settings in `.sln.DotSettings` (line wrapping disabled, abbreviations: JS, UI)
**Gitignore**: Standard .NET + `src/**/wwwroot/`, `node_modules/`, `**/package-lock.json` for ClientLib
**Multi-targeting**: Projects target net8.0 and net9.0
**Nullable**: Enabled (expect null handling requirements)
**XML Docs**: Enforced (CS1591 warnings normal)
**CPM**: All package versions in `Directory.Packages.props`

## Common Issues & Solutions

**NU1102** (Elsa packages not found): Update `ElsaVersion` in `Directory.Packages.props` to "Nearest version" from error
**NU1605** (Microsoft downgrade): Update `MicrosoftVersion` to match Elsa requirements (9.0.9+)
**Missing JS/CSS**: Build ClientLib npm assets before .NET build
**npm peer warnings**: Use `--force` flag (expected and safe)

## Making Code Changes

**Key Points**:
- Multi-target both net8.0 and net9.0
- All package versions in `Directory.Packages.props` (never in .csproj)
- Nullable reference types enabled - handle nullability
- XML docs enforced - expect CS1591 warnings
- After ClientLib TypeScript changes: run `npm run build` before .NET build
- Workflows.Designer uses @antv/x6 for visual graphs
- Localization uses ILocalizationProvider pattern with resx files
- Backend.Url in appsettings.json must point to Elsa API for auth

**Trust these instructions** - only search if you encounter undocumented errors or need module implementation details.

## Quick Reference

### Complete Clean Build
```bash
# Clean everything
dotnet clean Elsa.Studio.sln
rm -rf src/modules/Elsa.Studio.Workflows.Designer/ClientLib/node_modules
rm -rf src/framework/Elsa.Studio.DomInterop/ClientLib/node_modules

# Build npm assets
cd src/modules/Elsa.Studio.Workflows.Designer/ClientLib && npm install --force && npm run build && cd ../../../..
cd src/framework/Elsa.Studio.DomInterop/ClientLib && npm install --force && npm run build && cd ../../../..

# Build .NET
dotnet restore Elsa.Studio.sln
dotnet build Elsa.Studio.sln --configuration Release
```

### Package Publishing (CI Process)
```bash
# Pack NuGet packages (version will be from /p:Version parameter)
dotnet pack Elsa.Studio.sln --configuration Release /p:Version=3.7.0-preview.1 /p:PackageOutputPath=$(pwd)/packages/nuget

# Publish Custom Elements for npm
dotnet publish src/hosts/Elsa.Studio.Host.CustomElements --configuration Release -o ./packages/wasm /p:Version=3.7.0-preview.1 -f net8.0

# Pack npm wasm package (from packages/wasm/wwwroot after publish)
# Pack npm react wrapper (from src/wrappers/wrappers/react-wrapper)
```

### Solution Files
- Use `Elsa.Studio.sln` for full repository work
- Use `Elsa.Studio-Core.sln` for core framework development only (excludes some modules and samples)

### Environment Variables
None required for building. Backend URL is configured in appsettings.json files.
