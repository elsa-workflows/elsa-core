# Paired Blazor host and source debugging

Program #8194, feature #8215, story #8279, task #8284. Requires the [paired contract probe](paired-source-probe.md) from #8280/#8281.

Prepare a successful source-contract output first, then create the focused solution and host:

```sh
python3 scripts/integration-program/prepare_paired_blazor_host.py \
  --proof-output /path/to/new-disposable-proof
cd /path/to/new-disposable-proof/UiProbe
dotnet run --no-build --project UiProbe.csproj
```

The preparer requires the exact verified source pins, declared import patch and complete provider identity from the contract receipt. It refuses to overwrite an existing host/solution, builds `PairedDevelopment.sln` at net10, and verifies source state again afterward. The solution contains Core's Elsa project, both WorkflowContexts modules and the host, with their project-reference dependency closure. This is a focused development solution, not a claim that the entire consolidated Elsa solution is complete or buildable.

Open `http://127.0.0.1:6187`. If that port is already in use, stop your earlier disposable probe first; do not terminate an unrelated listener. The page renders the actual `WorkflowContextsEditor` and gets its provider from the real backend endpoint. After the Blazor circuit connects, the Synthetic checkbox starts unchecked with zero updates. Selecting it must display:

```json
{"Elsa:WorkflowContextProviderTypes":["SyntheticWorkflowContextProvider, UiProbe"]}
```

The callback count must become one. Deselecting must empty the list and increment the count to two. No database or workflow is saved: this sample intentionally keeps synthetic data in memory. It explicitly registers only the trusted synthetic provider type with Core's serializer, preserving the identifier expected by the legacy WorkflowContexts loader.

## Debug both source modules

Use the verified debugger release and archive hash in [the contract probe guide](paired-source-probe.md#source-breakpoint-check). From the prepared `UiProbe` directory, instead of `dotnet run`:

```sh
/path/to/netcoredbg --interpreter=cli \
  --command=/path/to/elsa-core/scripts/integration-program/paired-blazor-host/debug.commands \
  -- dotnet bin/Debug/net10.0/UiProbe.dll
```

Open the page once the host is listening. The debugger must hit the backend `List.ExecuteAsync` breakpoint and print its stack; the command file removes that breakpoint and continues. After the circuit connects, select Synthetic. It must hit `WorkflowContextsEditor.razor.cs:50`, print the component callback stack, remove that breakpoint and continue. The browser must then show the expected type identifier and update count. These are actual source breakpoints, not log markers.

Stop the host with Ctrl+C from its launch terminal. When using the command file, a graceful termination signal to the displayed disposable dotnet process lets it exit and the debugger execute `quit`. Do not leave a paused host or browser circuit running. The command file uses synchronous CLI behavior; adding `wait` after `run` can leave it waiting indefinitely.

## Boundaries and observed evidence

The sample binds loopback only and installs a synthetic permission identity for local demonstration. It does not configure production authentication, tenant authorization or persistent workflow storage. Do not deploy this host. The projects are nonpackable; the preparer has no publish or vendor-API path. The combined focused solution produced zero errors; existing source/dependency warnings and local-clone SourceLink warnings are retained in `blazor-build.log`. Local source debugging and publishable SourceLink are distinct proofs.

On 2026-09-23 root built the focused solution and verified selection/deselection in the prepared host through the browser. A prior identical disposable UI host also hit the actual component callback at line50, resumed to the browser metadata update, and exited0; the backend/service source breakpoints were independently hit as recorded in #8281. Root also verified the combined command file against the prepared host: backend breakpoint, component breakpoint, resumed browser selection and deselection, graceful exit0. See the [captured path-normalized transcript excerpt](../../scripts/integration-program/paired-blazor-host/debug-transcript.txt) and [observed result](../../scripts/integration-program/paired-blazor-host/observed-result.json). Build output alone does not assert browser or debugger success: the preparer reports both as unverified.

Repeat these checks against the final consolidated source layout before closing #8215/#8279. Preserve the complete-solution build and real host security configuration separately from this disposable development sample.
