# Feature packages and hosting

The Preview has 16 independent feature assemblies under `dotnet/modules/`.
Each owns its `Client/` source, ESM `entry.tsx`, CSS and CShells feature. The feature
registers a `ReactStudioModule` descriptor; only enabled server features appear in
`studio-manifest`. Disabled features expose neither manifests nor asset routes.
`module-catalog.json` is the first-party build inventory, not the runtime module list.

Build and validate from `studio/react/`:

```sh
pnpm install --frozen-lockfile
pnpm build
pnpm check:boundaries
dotnet build ../../Elsa.ReactStudio.slnf
dotnet test dotnet/Elsa.ReactStudio.Host.Tests
pnpm test:browser
```

The shared SDK, API client, auth and UI packages, React and third-party dependencies
are emitted once into the host's import map. Every feature entry retains bare imports
into that map, so providers, hooks and the designer share their original context
instances. The host checks the host/SDK API majors before importing an entry. Failed
registration rolls back contributions; a render error displays a module diagnostic
inside an error boundary and leaves the surrounding shell mounted.

The host compiles one ordered Tailwind utility stylesheet from first-party module
and shared UI sources. Repeating global utility rules in asynchronously loaded module
stylesheets can override responsive variants. `Client/module.css` owns additional
module-specific styles; use scoped selectors for extension styles. The host's CSS
also supplies the shared theme tokens. Empty module styles remain valid CSS resources.

Assets are served from `/_content/{PackageId}/{AssemblyVersion}/entry.js` and
`module.css` with immutable caching. HTML, configuration and manifests use `no-store`.
Both host and feature NuGet packs reject missing frontend assets. Production hosts
consume the prebuilt assets and need no Node runtime.

## Standalone hosting and feature selection

The standalone executable calls `builder.AddReactStudioPreview()` and
`app.MapReactStudioPreview()`. `ReactStudio:Path` defaults to `/studio-preview`.
All first-party server features are available by default. Set `ReactStudio:Modules`
to an array of module IDs to restrict optional features; Shell, Dashboard, Designer,
ActivityEditors, WorkflowDefinitions and WorkflowInstances remain enabled. Unknown
IDs fail configuration. Browser `features` flags and the connected backend's installed
feature catalog further restrict the visible optional UI.

An additional assembly supplies an `IShellFeature` that registers its descriptor
and can be added with `builder.AddReactStudioPreview(typeof(WeatherFeature))`.
Its client imports public `@elsa-workflows/react-studio-*` packages, exports
`register(api)`, and supplies compatible API majors. No feature may import host
aliases or another feature's source; `pnpm check:boundaries` enforces that invariant.

## Core server opt-in

Build the frontend first, then run from the repository root:

```sh
dotnet run --project core/src/apps/Elsa.Server.Web -- \
  --ReactStudio:Enabled=true
```

Core defaults to `ReactStudio:Enabled=false`. When enabled it serves Preview at
`/studio-preview` beside the existing `/elsa/api`. `ReactStudio:ServerUrl` defaults
to `/elsa/api`, so an absent browser configuration file uses the same-origin backend.
An explicit file or inline server list and `STUDIO_*` overrides take precedence;
an explicit empty server list is rejected. Other existing server endpoints keep their
normal routes. The Blazor Studio remains the default Studio throughout 3.x.

Standalone hosting can target a separate API origin using `STUDIO_SERVER_URL` and
the existing authentication settings. Configure that API's CORS policy for the
Studio origin. OIDC callbacks include the Studio mount; see [deployment](deployment.md).
