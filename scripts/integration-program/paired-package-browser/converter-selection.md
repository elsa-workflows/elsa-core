# Actual WebCIL converter selection

`paired_package_converter_selection` observes the binaries loaded during an owned
client build. It does not infer selection from SDK names, filenames, evaluated
properties, or the presence of another build's outputs. The tracked converter
policy remains the only production approval authority; this helper does not add
approved tuples.

The run owner prepares one private decoder outside the checkout and outside every
version/framework package cache:

```python
decoder = prepare_decoder(tool_root, layout.sdk, isolated_environment(layout))
commands = build(layout, converter_decoder=decoder)
selection = next(row["converter_selection"] for row in commands
                 if "converter_selection" in row)
converter = selection["converter"]
```

Pass `converter` to the existing candidate/baseline WASM resource derivation
helper. Retain the portable `selection` separately from private command records.
This module does not launch a host or claim browser execution. The root execution
adapter owns combining static package resources, managed WASM resources and real
browser responses. Non-Server builds without the explicit decoder fail before
SDK commands. Server's existing entrypoint and behavior are unchanged.

The decoder uses TraceEvent **3.2.8**, with a complete locked dependency graph.
`dependency-archives.json` independently pins the full signed archives against
official NuGet catalog SHA512/size records. NuGet lock `contentHash` has different
semantics: it is checked against restored metadata, never mislabeled as the hash
of full signed archive bytes. The reviewed inventory pins every selected compile,
runtime and build member. Restored cache bytes must match the original archive
members before compilation; runtime copies and the generated dependency closure
are checked after compilation. Source/build/output hashes and the unchanged
restored-assets hash bind decoder reuse. Decoder stamps use schema 2; older
preparations require a fresh private decoder directory. No ambient tool executable
is trusted. All preparation commands use normal `dotnet`, including the machine's
build-slot wrapper, are bounded to 180 seconds each, and must leave their owned
process group quiescent.

Client build capture requires POSIX process groups, no inherited EventPipe
configuration, disabled build servers/node reuse and shared compilation disabled.
The fresh trace directory receives startup `Microsoft-Windows-DotNETRuntime`
AssemblyLoader events. Python waits for the wrapper and its entire process group
to exit; it does not expect the Python build-slot wrapper itself to produce a
CLR trace. Timeout or a surviving child kills the entire owned group. Every
finalized trace must have a dead owner, parse fully with zero reported event loss,
and retain its exact filename/hash/length throughout decoding.

One trace/process must identify one task and one implementation result, with an
explicit exact-task requestor edge in the same reported target/bind context.
Identical-result CoreLib/Default path-load records are allowed only in the
[reviewed runtime path-loading pattern](https://github.com/dotnet/dotnet/blob/94ea82652cdd4e0f8046b5bd5becbd11461482ca/src/runtime/src/coreclr/vm/assemblynative.cpp#L111).
Their requested path must equal the result path. Arbitrary requestors, conflicting
result paths/identities/contexts, multiple owners, split traces, malformed Boolean
payloads and absent direct task edges fail closed. Bind-context equality is not
claimed as an independently emitted result-assembly context or exact activity
parent/child linkage. Unsuccessful bind probes establish no successful identity.

Actual loaded paths must be regular nonsymlink files under the isolated reviewed
`Microsoft.NET.Sdk.WebAssembly.Pack` 10.0.8 root and match the exact original
reviewed archive members. Their actual SHA256 values and execution SDK must match
the tracked policy. Source/wrapper authority is described in
`test/studio/browser/PackageCompatibility/webcil-converter-review.md`.

The returned command record adds `converter_selection`, containing the converter
tuple, package archive identity, trace filenames/hash/lengths, observed counts and
binding booleans, decoder source/binary hashes and verifier source hash. It
contains no absolute paths, raw bind-context strings, environment or credentials.
Raw traces, decoder JSON and command diagnostics remain in private directories
with files mode 0600. They are not portable evidence or artifact-upload inputs.

Verified build reuse re-decodes the unchanged original traces and rechecks current
cache/archive/policy/decoder bindings. Unverified preexisting WebCIL intermediates
require a fresh group: timestamp skips cannot substitute for a current selection
observation. Different net8/net9 manifest/runtime behavior still requires actual
validation; the helper does not certify those cells from a net10 observation.
