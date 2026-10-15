# Elsa Apps

The `apps/` product in the consolidated [Elsa repository](https://github.com/elsa-workflows/elsa-core) provides official application projects for running [Elsa Workflows](https://github.com/elsa-workflows/elsa-core) and [Elsa Studio](https://github.com/elsa-workflows/elsa-studio) as configurable ASP.NET Core applications.

Each project comes with its own `Dockerfile`, enabling you to run Elsa servers and studios as prebuilt, containerized applications.

## Projects

1. **Elsa Server**  
   An ASP.NET Core project representing a configurable Elsa Workflow Server application.

2. **Elsa Studio (Blazor WASM)**  
   An ASP.NET Core Blazor project representing a configurable Elsa Studio (WASM) application.

3. **Elsa Studio (Blazor Server)**  
   An ASP.NET Core Blazor project representing a configurable Elsa Studio (Blazor Server) application.

4. **Elsa Server + Studio (Blazor WASM)**  
   An ASP.NET Core project hosting both the Elsa Workflow Server and the Elsa Studio UI (WASM).

5. **Elsa Server + Studio (Blazor Server)**  
   An ASP.NET Core project hosting both the Elsa Workflow Server and the Elsa Studio UI (Blazor Server).

## Goal

The long-term goal is to evolve these projects into fully configurable Docker images.  
This will allow users to run Elsa Workflow Servers and Elsa Studio apps as prebuilt applications, configurable through:

- **Environment variables**  
- **Configuration files** (via mounts)

## Getting Started

From the consolidated repository root, use `apps/` as the Docker build context so the Dockerfile uses the product's own package and build configuration:

```bash
# Example: build and run the Elsa Server image with persistent SQLite storage
docker build -f apps/src/Elsa.Server/Dockerfile -t elsa-server-app apps
docker run -d -p 5000:8080 --name elsa-server \
       -e Http__BaseUrl=http://localhost:5000 \
       -e Identity__AdminUser__UserName=admin \
       -e Identity__AdminUser__Password='use-a-secret-password' \
       -e ConnectionStrings__Sqlite='Data Source=/data/elsa.sqlite.db;cache=shared' \
       -v "$PWD/data:/data" \
       elsa-server-app
```

## Using Docker Images from Docker Hub

```bash
# Example: run the Elsa Server image for Elsa 3.9.0
docker run --rm -p 5000:8080 \
       -e Http__BaseUrl=http://localhost:5000 \
       -e Identity__AdminUser__UserName=admin \
       -e Identity__AdminUser__Password='use-a-secret-password' \
       -e ConnectionStrings__Sqlite='Data Source=/data/elsa.sqlite.db;cache=shared' \
       -v "$PWD/data:/data" \
       elsaworkflows/elsa-server-app:3.9.0
```

The example uses SQLite, which is the configured default and the database path covered by the container smoke checks. MySQL provider builds currently report `NU1608` because the Pomelo EF Core 9 package is paired with EF Core 10; the MySQL combination has not been validated for these images.

Each Elsa Apps release publishes an exact version tag and a source-specific tag containing the Apps commit SHA for all six applications. Supported release tags are derived from the workflow input and must be Elsa 3.x SemVer versions. The hosted Studio WASM image also receives the matching `elsaworkflows/elsa-studio:<version>` alias, and the server image receives `elsaworkflows/elsa-server:<version>`. These aliases are limited to Elsa 3 release tags; the workflow does not update `latest` or Elsa 4 tags.

The [Core Apps validation workflow](../.github/workflows/apps-source-validation.yml) validates the nine net10.0 projects and builds/smokes all six image profiles on Linux AMD64 without pushing images. Run these local checks from the consolidated repository root:

```bash
dotnet build Elsa.Apps.slnf
python3 scripts/integration-program/verify_apps_source_import.py --evaluate
(cd apps && python3 -m unittest discover -s scripts/tests -v)
```

Apps keeps its released Core, Studio and Extensions package versions independently configurable. `apps/Directory.Build.targets` prevents source-reference substitution and excludes these application hosts/private shared libraries from Core NuGet packing. Existing `core/src/apps/` samples are a separate product surface.

The original [Apps publishing workflow](.github/workflows/container-images.yml) is retained under nested metadata and is **inert in Core**. The source repository's existing publisher remains operationally owned separately; this import does not activate a replacement, change registry/trust ownership, publish images, retire source authority or archive a repository. See the [source import record](../docs/integration-program/apps-source-import.md). The following correction procedure is retained reference for that separately governed source publisher.

For a correction to a completed publication, first merge the fix to `main`, then manually dispatch this workflow from `main` with the exact version and all three package versions, `images: all`, `publish: true`, the full reviewed `expected_commit`, and `supersede_run_id` set to the prior successful publication's run ID. The workflow verifies that run and its receipt artifact, builds and smokes all six profiles, and checks all eight existing version tags against the prior receipt before promoting. It allows only tags matching the prior receipt or the already-verified correction candidate, so an interrupted correction can be retried; a missing or unexpected tag blocks the correction. The new receipt links the superseded run, artifact digest, source commit, and prior image digests. The prior receipt remains intact, and this path never updates Elsa 4 or `latest` tags. Leave `supersede_run_id` blank for normal immutable publication, where conflicting version tags continue to be rejected.

Studio deployments should set the backend URL using `Backend__Url`, which is injected into the hosted client configuration as `window.elsaConfig.backendUrl`.

## License

[MIT](LICENSE)
