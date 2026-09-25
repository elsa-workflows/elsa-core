# Elsa Studio documentation

Studio's backend-facing clients, Blazor modules, hosts, and tests live in
[`src/studio`](../../src/studio) in the consolidated source tree. Open the
repository's canonical [`Elsa.sln`](../../Elsa.sln) to change the backend and
Studio together. The standalone Studio README retained in the
[import provenance](../integration-program/legacy/studio/README.md.source)
uses the former clone and solution paths; follow this page for the imported
layout.

Studio's Designer and DomInterop browser bundles use Node 22. From the
repository root, run the [reviewed ClientLib build script](../../scripts/integration-program/build_studio_clientlibs.sh)
and build the canonical solution:

```sh
./scripts/integration-program/build_studio_clientlibs.sh
dotnet build Elsa.sln
```

Start a backend such as [`Elsa.Server.Web`](../../src/apps/Elsa.Server.Web)
and run the [`Elsa.Studio.Host.Server`](../../src/studio/hosts/Elsa.Studio.Host.Server)
project in separate terminals to work on the Blazor UI. Its default backend URL is
`https://localhost:7294/elsa/api`; set `Backend:Url` in the host's ignored
`appsettings.Local.json` when your backend uses another address. The host
defaults to ElsaIdentity authentication. Keep developer credentials and client
secrets out of committed settings. The
[paired backend/Blazor proof](../integration-program/paired-blazor-host.md)
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
[asset disposition](../integration-program/consolidation/studio-performance-note-disposition.md)
before citing it as current behavior.

All three documents are byte-for-byte copies of the corresponding files at
Studio commit `f0eeb3c7428443b09512049fe890635fa4f7b427`. The first two
import dispositions and verification are recorded in the
[integration-program asset note](../integration-program/consolidation/studio-document-assets.md).
