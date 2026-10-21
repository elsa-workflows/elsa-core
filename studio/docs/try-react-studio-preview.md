# Try the React Studio Preview

The React interface is opt-in. Blazor remains the default Studio through Elsa 3.x.
Use the Preview alongside your existing Studio and API deployment. The sidebar
and browser title show **Preview**.

## Run from source

Install .NET SDK 10, Node.js 22.12 or newer and pnpm 10.33. From `studio/react`:

```sh
pnpm install --frozen-lockfile
pnpm build
```

Start your Elsa API, then run the standalone host. For a local API at port 5001:

```sh
STUDIO_SERVER_URL=http://localhost:5001/elsa/api pnpm host
```

Open `http://localhost:5201/studio-preview/` and sign in with an account from that
server. Allow the Studio origin in the API's CORS policy. Runtime settings come
from `studio.config.json`, server configuration and documented `STUDIO_*` overrides;
see [configuration](../react/docs/configuration.md) for authentication choices.
Never put a client secret into browser configuration.

Alternatively, enable Preview in the reference Core host after building the assets:

```sh
# From the repository root, alongside /elsa/api.
dotnet run --project core/src/apps/Elsa.Server.Web -- \
  --ReactStudio:Enabled=true
```

Open `/studio-preview/` on that host. `ReactStudio:Enabled` defaults to false.
Same-origin hosting uses `/elsa/api` unless explicit runtime configuration selects
another server. The existing Blazor application has its own deployment and URL.

## Consume the release packages

Once the release is published, add `Elsa.ReactStudio` version `3.11.0` to an
ASP.NET Core host. Until then, build a local feed with:

```sh
# Build JavaScript first, then from the repository root:
dotnet pack Elsa.ReactStudio.slnf -c Release -p:Version=3.11.0 -o /tmp/studio-feed
```

Configure that local directory as a NuGet source when restoring your host. Use the
same version for every `Elsa.ReactStudio.*` reference. A minimal host is:

```csharp
using Elsa.ReactStudio;

var builder = WebApplication.CreateBuilder(args);
builder.AddReactStudioPreview();
var app = builder.Build();
app.UseRouting();
app.MapReactStudioPreview();
app.Run();
```

Configure its API URL/authentication through runtime configuration. The default
mount is `/studio-preview`; built assets are embedded in the packages. Optional
server features can be restricted with `ReactStudio:Modules`, and browser feature
flags control their visible UI. See [feature packages](../react/docs/feature-packages.md)
and [deployment](../react/docs/deployment.md) for hosting details.

## Check the Preview

Create a workflow, add an activity, save and reload it. Inspect activity inputs,
settings and versions. Run a workflow and open its instance journal. Enable an
optional module only when the connected backend supports it and your account has
the required permission. The [parity audit](../react/docs/parity.md) lists confirmed
gaps; [release preparation notes](releases/3.11.0-react-studio-preview.md) distinguish
verified behavior from pending publication and delivery work.
