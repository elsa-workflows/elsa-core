# WorkflowContexts paired debugging in the consolidated source layout

Program #8194, feature #8215, story #8279. This follows the pinned three-repository [paired Blazor proof](../paired-blazor-host.md). The replay below uses the actual imported backend and Studio projects in the draft Core layout at `66f511e91a1e41a140e0001b7df03c8b83f1a78d`; it does not claim that the import has reached Core `main`.

## Reproduce the focused development loop

From the consolidated checkout, build the committed solution filter with source references:

```sh
dotnet build Elsa.WorkflowContexts.Debug.slnf -c Debug -p:UseProjectReferences=true
python3 -m unittest discover -s scripts/integration-program -p test_prepare_consolidated_paired_blazor_host.py
python3 scripts/integration-program/prepare_consolidated_paired_blazor_host.py \
  --output /absolute/path/to/new-disposable-host
cd /absolute/path/to/new-disposable-host/UiProbe
dotnet run --no-build --project UiProbe.csproj
```

The filter selects Core's `Elsa` project, the imported `Elsa.WorkflowContexts` backend and the imported `Elsa.Studio.WorkflowContexts` Blazor module from `Elsa.sln`. The preparer reuses the reviewed synthetic host and HTTP contract fixture from the pinned-source proof, maps their references to these three projects, refuses to overwrite an output or write into the source checkout, and writes a build receipt. It builds Debug/net10 with project references and package generation disabled. The receipt marks browser and debugger verification false until a person runs those steps; a successful build cannot imply them.

Open `http://127.0.0.1:6187/` after the host starts. The page renders the real imported `WorkflowContextsEditor`, which requests the real imported backend endpoint `GET /elsa/api/workflow-contexts/provider-descriptors`. With the synthetic provider registered, the checkbox starts unchecked and `Updates: 0`; selecting it shows `Updates: 1` and `{"Elsa:WorkflowContextProviderTypes":["SyntheticWorkflowContextProvider, UiProbe"]}`; deselecting shows `Updates: 2` and an empty provider list. The endpoint should return HTTP 200 and a `Synthetic` provider descriptor. This fixture is loopback-only and keeps its workflow definition in memory.

To repeat the two source breakpoints, use the verified `netcoredbg` release described in the [pinned probe guide](../paired-source-probe.md#source-breakpoint-check) with `scripts/integration-program/paired-blazor-host/debug.commands`, launching the built `UiProbe.dll` from the disposable `UiProbe` directory. Load the browser page to hit `Elsa.WorkflowContexts.Endpoints.ProviderTypes.List.List.ExecuteAsync` in `Endpoint.cs:28`; continue. Select the checkbox to hit `WorkflowContextsEditor.OnCheckChanged` in `WorkflowContextsEditor.razor.cs:50`; continue and observe the metadata update. Stop the host, ensure the listener is gone, and remove the disposable output when no longer needed. Do not use the synthetic identity or host for deployed security validation.

## Observed on the draft import

On 2026-09-26, the focused filter built Debug for net8.0, net9.0 and net10.0 with zero errors and 64 inherited warnings using .NET SDK 10.0.300. A disposable net10 host built with zero errors and 21 inherited warnings. Root observed the endpoint's HTTP 200 response and the exact checkbox/update sequence above in the browser. In one debugger run, the backend and Blazor source breakpoints both stopped at their imported source files, the browser reflected the selection after continuation, and the debugger exited with code 0. The [structured receipt](../../../scripts/integration-program/consolidated-paired-debug-observed.json) records this observed result without copying the raw local debugger log.

This proves paired source development and debugging for one representative backend/Studio module pair. It does not establish production authentication, tenant authorization, persisted workflow behavior, a complete `Elsa.sln` pass, or final-head compatibility. Replay the filter, host and browser/debugger checks on the final import head before closing #8279/#8215; retain the separate full-solution gate and publisher/cutover approvals.
