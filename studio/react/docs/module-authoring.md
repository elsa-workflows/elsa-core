# Author a React Studio module

React Studio Preview accepts independently built Razor class libraries. A module
packages an ESM entry and scoped CSS, registers a server feature, and contributes
through the public SDK. The host supplies React, UI, auth and API contexts. This
guide and sample implement [#8852](https://github.com/elsa-workflows/elsa-core/issues/8852).

## Generate and build the sample

Install .NET SDK 10, Node.js 22.13 or newer and pnpm 10.33. From `studio/react`, run:

```sh
node scripts/create-module.mjs /tmp/weather-module --company Acme --name Weather
cd /tmp/weather-module/Module/Client
pnpm install
pnpm build
cd ../..
dotnet pack Module/Module.csproj -c Release -o artifacts/packages
```

The generator accepts a new or empty output directory and never overwrites files.
Output must be outside the source template, including paths through symbolic links.
Use `--studio-version` and `--npm-version` to select matching published versions;
the default is 3.11.0. The SDK packages are currently unpublished, so the default
registry install requires the release work in #8858. To verify the sample now,
use local packages from a repository checkout:

```sh
# From studio/react, build JavaScript before embedding it in NuGet packages.
pnpm install --frozen-lockfile
pnpm build
dotnet pack ../../Elsa.ReactStudio.slnf -c Release \
  -p:Version=3.11.0-preview.8852 -o /tmp/studio-feed
pnpm exec playwright install chromium
pnpm test:module --nuget-feed /tmp/studio-feed --studio-version 3.11.0-preview.8852
```

The verification generates outside the repository, installs packed npm tarballs,
packs the generated RCL, and restores a host from local NuGet feeds. Neither
generated project uses project references. Chromium checks both contributions,
shared contexts, activity save/read-only behavior and server feature disabling.
It cleans up the temporary host and package cache. Set
`PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH` to use an installed Chromium executable.

For your own host, add your module and Elsa package feeds to `NuGet.Config`, then
run `dotnet run --project Host --urls http://127.0.0.1:5204` from the generated
directory. Open `/studio-preview/`. Configure the API origin, CORS and authentication
before connecting to a protected server; the sample uses anonymous development auth.

## Register contributions

`ModuleFeature.cs` registers a `ReactStudioModule` with a stable module ID, package
ID, assembly and host/SDK API majors. Its `IShellFeature` depends on
`ReactStudioPreviewFeature`. The host calls
`builder.AddReactStudioPreview(typeof(WeatherFeature))` and enables `acme-weather`
in `studio.config.json` browser features. Both server registration and browser
enablement are required. Removing the server feature removes its manifest entry
and asset routes; hiding browser UI alone does not remove server assets.

The ESM entry exports `register(api: StudioApi)`. The generated sample contributes
to `dashboardWidgets` and `activityPropertiesTabs` using typed `StudioSlots` props.
Use the [SDK contracts](../packages/sdk/src/slots.ts) and generated `pnpm docs:api`
reference for signatures; a declared slot is usable only where the installed host
renders it. Use module-local contribution IDs. Contributions sort by order, owner
ID and contribution ID. An omitted order is zero; built-in UI hint contributions
use 1000 so extensions can override them with the default order.

Read data through `useElsaClient` and `useAuth` rather than creating separate
providers. Activity tabs must honor `context.readOnly` and use
`context.updateActivity` with immutable copies that retain unrelated properties.
Persist extension data under a module-specific custom-property key. Enforce API
permissions on the server as well as in the interface.

The host owns registrations and removes them on disposal. Register cleanup for
timers, subscriptions and other external resources with `api.onDispose`; cancel
outstanding work when appropriate. Registration errors roll back contributions.
Render failures display a module diagnostic. Keep side effects outside render.

## Package assets and compatibility

`Module/Client/vite.config.ts` leaves React, React DOM and public Studio package
imports external. The host import map resolves them to shared runtime instances.
Bundle private dependencies if necessary; do not import host aliases, first-party
module source, or a second React runtime. Commit the generated pnpm lockfile and
use frozen installs after the initial dependency resolution.

The RCL embeds `wwwroot/entry.js` and `module.css` as
`ReactStudio.Module/<asset>`. Its build and pack reject missing assets. Build the
client before .NET so the package contains the latest output. The sample targets
.NET 8, 9 and 10; its host targets .NET 10. Production consumers need no Node runtime.

Scope selectors to `.acme-weather` and use shared theme variables. Avoid global
resets or an additional Tailwind preflight; host utility classes are compiled from
host sources, so ship extension-specific rules in your own stylesheet.

The current host API and SDK API majors are both 1. Incompatible majors are
rejected before importing an entry. Test your module against each supported host
release. Module asset URLs include the package assembly informational version and
use immutable caching: increase your NuGet version whenever shipped JS/CSS changes.
Do not replace published package contents under an existing version. Review your
module's metadata, notices and license before distribution.
