# Reviewed WebCIL converter

The policy approves one conversion implementation for SDK **10.0.300**, reviewed
by the workroom lead and an independent Sol worker on 2026-10-06 for Core #8643.
It does not accept a browser cell or establish which converter a later build
selected. Production orchestration must verify the actual selected task and
implementation binaries against this tuple before using it. Non-Server execution
remains blocked at that orchestration seam until its runtime evidence is wired.

## Package and source authority

The isolated package is `Microsoft.NET.Sdk.WebAssembly.Pack` **10.0.8**, archive
size 4,549,168 bytes and SHA-256
`73a7820139c952a9c8c22197d83a87540f1af38d4ae411c3ec97a1908b87cd23`.
Its [NuGet catalog record](https://api.nuget.org/v3/catalog0/data/2026.05.12.18.17.51/microsoft.net.sdk.webassembly.pack.10.0.8.json)
identifies the official repository commit
`94ea82652cdd4e0f8046b5bd5becbd11461482ca`.

The task binary is `tools/net10.0/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks.dll`
(SHA-256 `dc7516b7299f29586384e546d3fa059cfe5e014258928e91f9d78359fab2f5a0`).
The implementation binary is `tools/net10.0/Microsoft.NET.WebAssembly.Webcil.dll`
(SHA-256 `3ad18f76f32f509b801df768457d65d86d534fbdd02247a07ac093795a920bd3`).
Both actual loaded paths were regular files in the isolated package cache and
matched the exact original archive members byte for byte.

The pinned [task project](https://raw.githubusercontent.com/dotnet/dotnet/94ea82652cdd4e0f8046b5bd5becbd11461482ca/src/runtime/src/tasks/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks.csproj)
compiles the [adapter](https://raw.githubusercontent.com/dotnet/dotnet/94ea82652cdd4e0f8046b5bd5becbd11461482ca/src/runtime/src/tasks/WasmAppBuilder/WebcilConverter.cs)
and references the implementation. The [conversion task](https://raw.githubusercontent.com/dotnet/dotnet/94ea82652cdd4e0f8046b5bd5becbd11461482ca/src/runtime/src/tasks/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks/ConvertDllsToWebCil.cs)
calls that adapter, which delegates directly to the implementation.
`source_sha256` means the exact UTF-8 bytes of
[WebcilConverter.cs](https://raw.githubusercontent.com/dotnet/dotnet/94ea82652cdd4e0f8046b5bd5becbd11461482ca/src/runtime/src/tasks/Microsoft.NET.WebAssembly.Webcil/WebcilConverter.cs),
not a digest learned from generated output.

The [wrapper source](https://raw.githubusercontent.com/dotnet/dotnet/94ea82652cdd4e0f8046b5bd5becbd11461482ca/src/runtime/src/tasks/Microsoft.NET.WebAssembly.Webcil/WebcilWasmWrapper.cs)
has SHA-256 `715729fa186750a93154a8a32a78d70ebe8b7f63fec6b74eb3f3c72839b1256c`.
Its 157-byte prefix and 29-byte suffix independently reproduce the policy's
outside-data wrapper hash. The parser requires the exact two-global/four-export
wrapper, canonical lengths, section ordering, data-segment shape and alignment.
PE section content must remain identical except for the source-defined debug
Characteristics zeroing and conditional pointer adjustment. Header and relocated
section offsets are checked independently; no arbitrary code or data mutation is
permitted. This is a reviewed package/source chain, not a reproducible-build claim.

## Actual cold-build evidence

Lead fixture commit `c9ac65769783726e8847b40bb920fd1365a29322` built the original
unpublished 3.10.0 candidate's net10.0 WASM fixture from a fresh isolated group.
All SDK commands passed, their owned process groups became quiescent, and strict
candidate package closure verification passed. No application host was started.

The private binlog identifies `ConvertDllsToWebCil` TaskId 84, the exact task
path above, external NET/arm64 task-host execution, actual input/output mapping,
and successful completion. Three finalized EventPipe traces parsed with zero
reported lost events. All trace-owner PIDs were absent after execution and the
trace byte inventory stayed unchanged through decoding.

One trace/process reported one successful task load and two successful WebCIL
loads with the same result name, path, hash and reported target/bind context.
One WebCIL record explicitly names the exact task assembly as its requestor.
The other names CoreLib/Default: pinned
[runtime path-loading source](https://raw.githubusercontent.com/dotnet/dotnet/94ea82652cdd4e0f8046b5bd5becbd11461482ca/src/runtime/src/coreclr/vm/assemblynative.cpp)
attributes path loads to CoreLib while retaining the supplied custom binder.
This is one result identity, not conflicting converter binaries. The decoder's
overly strict requestor-cardinality rejection is preserved in the raw evidence.
Exact parent/child activity linkage was not decoded, and reported bind context
is not described as a separately observed result-assembly context.

The original capture driver also rejected the absence of a .NET trace for the
Python build-slot wrapper PID. The installed wrapper forks and waits for the
real dotnet process in the same group. That failed receipt remains unchanged;
post-capture analysis used the original finalized child traces without rebuilding.

The cold group's sealed package PE → generated WebCIL → served build output
comparison passed for **41 package DLLs plus one separately owned fixture**.
It checked exact build-manifest mapping, output size/integrity and PE section
content. A subsequent replay passed using the tracked converter policy without
a test-policy override. This was a build-output check; no actual WASM browser
request or execution was demonstrated.

Private evidence is retained locally; these hashes bind the reviewed records
without publishing raw paths, environment, logs or trace payloads:

| Record | SHA-256 |
| --- | --- |
| Original failed driver receipt | `e7a7d87b826d03f37143325e6a61c79b7dc947c28b68c34935daeffe2aa0d937` |
| Post-capture decoder receipt | `bf7add29bd92c3fe3f67648768b33578f362b5fcc1f8a743d9167209509f177f` |
| Loaded archive-member comparison | `d0979f8ce51a4fc7d4a7fc2d8fdd51a2ab276a7d8b61b01305a54b4d2e098808` |
| Build binlog | `58d10803ddf71ec41e327c584d1e86d14b3f0680e83d7fbc45945017b28b01ee` |
| Conversion/build-output comparison | `c0e8f42bfdb70b0547283e985a5385398e294cb5f78a4831eb40b967dc7e00f4` |
| Tracked-policy conversion replay | `96207570a405e3a295f07a34357b1e9ef0ee2ed297aaecc9838808b1da3c54d1` |
