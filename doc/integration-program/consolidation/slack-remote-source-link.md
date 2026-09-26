# Slack symbol provenance from the imported Core repository

Program #8194; package proof #8260. The nonpublishing [artifact-only Slack run 36215703520](https://github.com/elsa-workflows/elsa-core/actions/runs/36215703520) built at draft import commit `8f85a3db84af3fac12df5f4364f285b70c761451`. It retained exactly one local `Elsa.Slack` nupkg/snupkg pair at proof version `3.8.5-proof.36215703520.1`:

| Artifact | SHA-256 |
| --- | --- |
| `.nupkg` | `25dacf609c85edb58274bacd6e105a41a3a5d1d505aa5ba5d1024df864b767e0` |
| `.snupkg` | `8bc167bddc9f74a7b60331f1a06c79363102d55728e8341ade5ae457293384cf` |

The package repository metadata names `https://github.com/elsa-workflows/elsa-core` at that exact commit. SourceLink CLI 3.1.1, whose installed payload was checked against its package bytes (SHA-256 `169289a8b8340cf1d982b050b31d7fce116fddcedfd434cee542ccca3cf6b1dd`), found the same mapping in the net8.0, net9.0 and net10.0 PDBs: `https://raw.githubusercontent.com/elsa-workflows/elsa-core/8f85a3db84af3fac12df5f4364f285b70c761451/*`. `sourcelink test` passed URL and source-checksum verification for all three PDBs, each containing 41 source documents. The package verifier additionally matched each `.nupkg` assembly to its `.snupkg` Portable PDB using .NET's associated-PDB check. The retained pair passed for all three frameworks; a cross-framework PDB swap failed before SourceLink verification. A deliberate wrong-commit check also failed before producing a receipt.

The `Elsa.Slack release-unit artifact rehearsal` workflow now runs [`verify_slack_source_link.py`](../../../scripts/integration-program/verify_slack_source_link.py) after its clean package consumer on same-repository PRs and manual runs. It installs pinned SourceLink 3.1.1 from the job's NuGet.org-only config, verifies the tool payload, binds each packaged assembly to its external symbols, requires actual source documents, checks each symbol file against the exact PR-head commit, and uploads a separate JSON receipt with artifact, tool and document-count evidence. Fork PR commits are not yet reachable from `elsa-core`, so their Core URL check is explicitly deferred; the ordinary package artifacts are still retained, and a same-repository run is required before release acceptance. A changed artifact or requested commit must pass a fresh run; the historical hashes above are not a release allocation.

To recheck the retained pair locally after downloading it into an otherwise empty directory:

```sh
dotnet tool install --tool-path /tmp/elsa-sourcelink sourcelink --version 3.1.1 --configfile /path/to/nuget-org-only-config
python3 scripts/integration-program/verify_slack_source_link.py \
  --artifacts /path/to/downloaded-pair \
  --version 3.8.5-proof.36215703520.1 \
  --commit 8f85a3db84af3fac12df5f4364f285b70c761451 \
  --sourcelink-tool /tmp/elsa-sourcelink/sourcelink \
  --output /tmp/elsa-slack-source-link-receipt.json
```

This is remote source provenance for one draft-head artifact. It does not establish a released package, final `main` commit, complete source dependency closure, publisher cutover, or permission to publish. Those #8260 gates remain open.
