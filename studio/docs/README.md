# Elsa Studio documentation

Studio's backend-facing clients, Blazor modules and hosts live under
[`studio/src`](../src), with browser fixtures under [`studio/test`](../test),
existing .NET tests beside their modules/framework under `studio/src/`, and samples under
[`studio/samples`](../samples). Open the repository's canonical
[`Elsa.sln`](../../Elsa.sln) to change backend and Studio together, or use the root
`Elsa.Studio.slnf` for a focused selection including required dependencies. Read
[Studio contributor guidance](../src/AGENTS.md) and the root
[contribution guide](../../CONTRIBUTING.md).

The standalone Studio README retained in the
[import provenance](../../docs/integration-program/legacy/studio/README.md.source)
uses former clone and solution paths; follow this page for current development.

Studio's Designer and DomInterop browser bundles use Node 22. From the
repository root, run the [reviewed ClientLib build script](../../scripts/integration-program/build_studio_clientlibs.sh)
and build the Studio selection:

```sh
./scripts/integration-program/build_studio_clientlibs.sh
dotnet build Elsa.Studio.slnf
dotnet test studio/src/modules/Elsa.Studio.Workflows.Designer.Tests/Elsa.Studio.Workflows.Designer.Tests.csproj
```

The Community combined image host is
[`Elsa.ServerAndStudio.Web`](../src/hosts/Elsa.ServerAndStudio.Web). It serves
the Elsa Server API and Blazor Server Studio in one process. Same-process Studio
must call the in-container API (`Backend__Url=http://127.0.0.1:8080/elsa/api`),
not a host-mapped port. HTTP endpoint start/resume stays on `/workflows/{path}`,
including existing `definitions/...` and `instances/...` callbacks. When no HTTP
trigger or bookmark matches, exact Studio pages (`/workflows/definitions`,
`/workflows/definitions/{id}/edit`, `/workflows/instances`,
`/workflows/instances/{id}/view`) fall through to Blazor. A published HTTP
workflow at an exact Studio page URL hides that Studio page for the matching
method; choose a distinct HTTP path if both are needed. The combined host
registers a single unmatched handler (last registration wins). That registration
opts this host into lookup-then-reject during pause or drain: matched HTTP start
and resume still return 503 without executing work, unmatched Studio pages remain
available, and unmatched non-page paths stay 404. Hosts without the handler keep
the original pre-lookup 503.
The dashboard is `/` and `/dashboard`. Workflow and identity persistence share
`elsa_workflows.db`.
`docker/ElsaServerAndStudio.Dockerfile` publishes this host after generating
Studio ClientLib assets.

Start a backend such as [`Elsa.Server.Web`](../../core/src/apps/Elsa.Server.Web)
and run the [`Elsa.Studio.Host.Server`](../src/hosts/Elsa.Studio.Host.Server)
project in separate terminals to work on the Blazor UI. Its default backend URL is
`https://localhost:7294/elsa/api`. The checked-in `Elsa.Server.Web` launch
profile instead listens on `https://localhost:5001`; for this pairing, put
the following in the Studio host's ignored `appsettings.Local.json`:

```json
{
  "Backend": {
    "Url": "https://localhost:5001/elsa/api"
  }
}
```

The host defaults to ElsaIdentity authentication. Keep developer credentials and client
secrets out of committed settings. The
[paired backend/Blazor proof](../../docs/integration-program/paired-blazor-host.md)
records the bounded source-breakpoint demonstration; it is not a production
host configuration.

The [interface design system](design/system.md) is the Studio-approved baseline
dated 2026-09-01. It describes UI conventions for Studio contributors; evaluate
new work against the current module and its tests as well.

The [3.6.0-rc1 release notes](releases/3.6.0-rc1.md) are retained release
history. They describe that release candidate, not the current release or a
consolidated Core release.

The [develop 3.6 performance report](history/performance-optimizations-develop-3.6.md)
is also retained as historical evidence. Its measurements, code locations and
claims of implemented fixes describe the source at the time; they have not
been revalidated for the consolidated Studio modules. See the
[asset disposition](../../docs/integration-program/consolidation/studio-performance-note-disposition.md)
before citing it as current behavior.

All three documents are byte-for-byte copies of the corresponding files at
Studio commit `f0eeb3c7428443b09512049fe890635fa4f7b427`. The first two
import dispositions and verification are recorded in the
[integration-program asset note](../../docs/integration-program/consolidation/studio-document-assets.md).

## Planned native compensation

The [Native compensation authoring, inspection and recovery program](../../core/docs/compensation/README.md) is the current program entrypoint for scope, ownership and readiness. The [Studio interaction plan](specs/native-compensation/plan.md) records Studio design details within the reviewed [Elsa 3 adaptation packet](../../core/docs/specs/native-compensation/README.md). Standing owner authorization permits delivery through the remaining technical gates; these plans do not claim implementation or proof.
