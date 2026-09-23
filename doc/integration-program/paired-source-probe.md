# Paired WorkflowContexts source contract probe

Program #8194, feature #8215, story #8279, task #8280.

This disposable harness builds the actual backend and Blazor Studio WorkflowContexts modules with project references into Core and Studio. It verifies the Studio `RemoteFeature` name against the backend installed-feature registry, then calls Studio's `RemoteWorkflowContextsProvider` over loopback HTTP through the real API client and backend endpoint. A synthetic provider must return the descriptor `Synthetic` exactly once.

The harness is a bounded precursor to the consolidated root solution, interactive Studio and debugger acceptance. It does not establish those capabilities by itself.

## Reproduce

Install .NET 10 and provide local Git repositories containing these commits:

| Repository | Commit |
| --- | --- |
| Core | `22f479d423975cf5d35f12fa5204b6597764d06b` |
| Extensions | `33fa0bfd28c7585240e3d4f665058c067b17e287` |
| Studio | `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` |

```sh
python3 scripts/integration-program/run_paired_source_probe.py \
  --core-source /path/to/elsa-core \
  --extensions-source /path/to/elsa-extensions \
  --studio-source /path/to/elsa-studio \
  --output /path/to/new-disposable-proof
```

Run on macOS or Linux (the timeout cleanup uses POSIX process groups). The output directory must not exist. The script creates local shared Git clones, checks out the exact commits, and leaves the caller's source checkouts unchanged. Keep those source repositories available while using the disposable clones because their object databases are shared. Package restore requires access to the sources configured by the pinned repositories. No publish command, package feed write or external connector API call occurs.

The pinned Extensions Studio module has an unused `@using Blazored.FluentValidation` that fails compilation because the referenced namespace is absent. The harness removes exactly that line in its disposable clone, records before/after hashes, and rejects a missing or duplicated line. This is an explicit compatibility correction, not an upstream source change or a claim that the unmodified baseline compiles. It must be carried into the eventual imported source. Both modules target net10 for this proof; other target frameworks remain outside this receipt.

The ephemeral host binds `127.0.0.1` on an assigned port and installs a synthetic local identity with only the endpoint's read permission. This identity exists only in the probe; production authorization and tenant isolation are not exercised. Studio's module service and actual client execute, but the Blazor component is not rendered. The host stops in `finally`; the outer process has a 15-minute timeout.

## Evidence and next acceptance

`build.log` retains compiler warnings, HTTP trace and teardown. `evidence.json` records source pins, the single patch, assembly paths, feature matching and the descriptor result. Source pins and tracked/untracked changes are checked again after execution. Existing warnings from the pinned source graph are not treated as new findings or suppressed.

After this contract is established, the parent story still requires the focused root solution/filter, a running Blazor host, source breakpoints on both sides, preserved full-solution build behavior, and the same demonstration against the consolidated layout. Do not close #8215 on this probe alone.

The 2026-09-23 run passed from fresh disposable clones: matching `Elsa.WorkflowContexts`, HTTP 200, exactly one `Synthetic` descriptor, expected source patch only, and host shutdown. The portable [captured receipt](../../scripts/integration-program/paired-source-probe/observed-result.json) omits machine-specific assembly paths; regenerated receipts retain them. Both initial and final-guard runs passed. Nineteen repository tool tests also passed.

## Source breakpoint check

Root additionally ran the built probe under [NetCoreDbg 3.2.0-1092](https://github.com/Samsung/netcoredbg/releases/tag/3.2.0-1092) on macOS arm64. The official `netcoredbg-osx-arm64.zip` SHA-256 was `f4fa33b3ff874910cc184b4bb3b9c56d0abdf5c6521cee0b144d7c6e4a6e59ea`; the executable reports `3.2.0-1 (9744e1f, Release)`. No global debugger installation is required. Verify the downloaded tool's provenance before using it; the harness does not download or execute a debugger automatically.

Using the [CLI command file](../../scripts/integration-program/paired-source-probe/debug.commands) with the built output:

```sh
/path/to/netcoredbg --interpreter=cli \
  --command=/path/to/elsa-core/scripts/integration-program/paired-source-probe/debug.commands \
  -- dotnet /path/to/new-disposable-proof/ContractProbe/bin/Debug/net10.0/ContractProbe.dll
```

The successful run resolved and hit Studio `RemoteWorkflowContextsProvider.cs:27` before the HTTP call, then backend `List.ExecuteAsync` at `Endpoint.cs:28`. Both stack traces contained the respective module and source file; continuing yielded the expected descriptor receipt and exit code 0. The recorded [breakpoint summary](../../scripts/integration-program/paired-source-probe/debug-observed.json) distinguishes this from interactive Blazor: no Razor component/browser breakpoint has been demonstrated yet. A breakpoint in the Studio service establishes service-source debugging only.

The command file follows the debugger's synchronous CLI mode: no `wait` command is needed after `run` or `continue`. Initial attempts using a non-executable source line and an unnecessary `wait` did not provide acceptance evidence and were discarded with their processes terminated. When running manually, use `quit` if execution stalls and confirm the disposable probe process exits.

## Provider type compatibility requirement

The pinned Core serializer intentionally emits `UnregisteredClrType:` metadata for unregistered types. Extensions' current context loader still expects a simple assembly-qualified name. The host must explicitly register its trusted provider in `SerializationTypeOptions` using that identifier. The probe now does so and asserts the full returned identifier resolves to exactly its known synthetic provider type; checking only the descriptor display name is insufficient. This is sample host configuration, not a change to the serializer's security boundary. It does not authorize resolving arbitrary caller-supplied CLR type names.

A separate temporary interactive Blazor experiment rendered the actual WorkflowContextsEditor, selected and deselected its Synthetic checkbox, and observed callback counts 1 and 2 with corresponding workflow metadata updates. That experiment revealed the type-registration requirement above. It is not yet part of this reproducible contract harness and does not close the parent UI/debugger acceptance.
