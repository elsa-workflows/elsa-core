# Elsa Templates

`elsa-templates` provides .NET solution templates for getting started with Elsa quickly.

## Version Policy

The `main` branch targets the latest stable Elsa release. The current stable release is Elsa `3.9.0`.

Preview and release-candidate work uses a matching `release/<base-version>` branch, for example `release/3.9.0`, and produces versions such as `3.9.0-preview.1234`.

The package workflow publishes stable packages only for a published release whose tag is reachable from `main`. Prerelease and release-candidate tags must be reachable from their matching `release/<base-version>` branch and publish to the preview feed and NuGet. Pushes to `main` produce preview packages on the preview feed. Keep preview work on its matching version branch and do not replace the latest stable package with a prerelease build.

## Available Templates

- `elsa-server`: Elsa Server with selectable feature model and EF Core persistence provider.
- `elsa-studio`: Elsa Studio with selectable Blazor hosting model, authentication provider, and optional Labels module.
- `elsa-combined`: Elsa Server and Elsa Studio in one solution with selectable server feature model, persistence provider, Studio hosting model, Studio authentication provider, and optional Labels module.

## Install From NuGet

Stable template packages are published to NuGet.org:

```bash
dotnet new install Elsa.Templates@3.9.0
```

## Install From Preview Feed

Preview template packages are published to the Elsa preview feed. Install a preview version by passing the feed URL as an additional source and specifying the preview version:

```bash
dotnet new install Elsa.Templates@<preview-version> --add-source https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json
```

For example: `Elsa.Templates@3.9.0-preview.1234`.

To make the preview feed available for package restore as well, add it to your `NuGet.config` next to the main NuGet feed:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Elsa 3 Preview" value="https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json" />
  </packageSources>
</configuration>
```

## Install From Source

```bash
dotnet pack src/Elsa.Templates/Elsa.Templates.csproj
dotnet new install artifacts/package/release/Elsa.Templates.*.nupkg
```

## Usage

Create an Elsa Server using static feature registration:

```bash
dotnet new elsa-server -n MyElsaServer --feature-model static
```

Create an Elsa Server using CShells:

```bash
dotnet new elsa-server -n MyElsaServer --feature-model shell
```

Create an Elsa Server using a different EF Core persistence provider:

```bash
dotnet new elsa-server -n MyElsaServer --persistence sqlite
dotnet new elsa-server -n MyElsaServer --persistence sqlserver
dotnet new elsa-server -n MyElsaServer --persistence postgresql
dotnet new elsa-server -n MyElsaServer --persistence oracle
```

Create an Elsa Studio solution:

```bash
dotnet new elsa-studio -n MyElsaStudio --hosting server
dotnet new elsa-studio -n MyElsaStudio --hosting wasm
dotnet new elsa-studio -n MyElsaStudio --hosting hybrid
```

Configure Studio authentication and optional modules:

```bash
dotnet new elsa-studio -n MyElsaStudio --auth-provider elsa-identity
dotnet new elsa-studio -n MyElsaStudio --auth-provider open-id-connect
dotnet new elsa-studio -n MyElsaStudio --auth-provider elsa-login --with-labels
```

Create a combined Elsa Server + Studio solution:

```bash
dotnet new elsa-combined -n MyElsaApp --feature-model static --studio-hosting server
dotnet new elsa-combined -n MyElsaApp --feature-model shell --studio-hosting wasm
dotnet new elsa-combined -n MyElsaApp --feature-model shell --studio-hosting hybrid --persistence postgresql --auth-provider open-id-connect --with-labels
```

Hybrid Studio output includes a host project and a WASM client project. The generated host reads `Studio:HostingModel` from configuration so the runtime can start Studio as `Server` or `Wasm`.

The stable `3.9.0` template package exposes only options that restore and build against stable Elsa packages. Preview-only Studio modules and the current MySQL EF provider are intentionally not exposed from `main`.

## Configure Identity

The static feature model uses the configuration-backed identity providers. The generated development settings include a sample `admin` user and application for local use; remove those samples and provide deployment-owned values before production. The static provider reads `Identity:Roles`, `Identity:Users`, and `Identity:Applications` from configuration.

The shell feature model uses CShells identity and persistence features. In development, `CShells:Shells:Default:Features:DefaultAdminUser` bootstraps the configured administrator, including `AdminUserName`, `AdminPassword`, `AdminRoleName`, and `AdminRolePermissions`. Set the signing key at `CShells:Shells:Default:Features:Identity:SigningKey` through deployment configuration and replace the development password before deployment.

Production has no built-in credentials. Choose one identity source for each deployment: use configuration-backed providers for a static setup, or use the durable identity provider with a deployment-owned administrator bootstrap for a shell setup. Do not combine configuration-backed users with a durable provider and assume they are interchangeable.

## Verification

The smoke tests pack the template package, install it into an isolated template hive, generate a representative matrix covering the supported options, and build the generated outputs.

```bash
dotnet test test/Elsa.Templates.Tests/Elsa.Templates.Tests.csproj
```

## Development

See [Roadmap](docs/roadmap.md) and [Tasks](docs/tasks.md).
