# Imported Studio package provenance and shared icon

Program #8194; story #8286; task #8443. This proof runs on the history-bearing draft import #8409. It creates only local/CI artifacts and does not enable a publisher.

At import head `036d4e8394cfd89a45c2316aa0f305e7da90123b`, a local `Elsa.Studio.Core` pack showed a real metadata mismatch: `projectUrl` and `repository.url` still named `elsa-studio`, while `repository.commit` named the Core import commit. The imported `src/studio/Directory.Build.props` supplied the old URLs. The same archive included `icon.png` with SHA-256 `82fd76d734d59efc6132af0b0b999146254fa5a296ea5d64f85597bb1cda524e`, identical to Core root and the retained Extensions/Studio icons. The [hosted Slack artifact rehearsal](https://github.com/elsa-workflows/elsa-core/actions/runs/36136264206) independently produced the same icon bytes in `Elsa.Slack`.

The scoped Studio build properties now point package project/repository metadata to `elsa-core`. The [artifact-only workflow](../../../.github/workflows/prove-studio-package-provenance.yml) packs `Elsa.Studio.Core` with a `0.0.0-proof` version and read-only permissions. It has PR/manual triggers, no push/release trigger, no feed credentials, and no publish command. The [verifier](../../../scripts/integration-program/verify_studio_package_provenance.py) requires one nupkg/snupkg pair, unchanged package ID, three framework assemblies and PDBs, the Core repository URL and exact source commit in nuspec, and the reviewed icon bytes. CI also runs pinned SourceLink 3.1.1 URL/content checks on every PDB before retaining short-lived artifacts.

To repeat locally from a clean checkout at the PR head:

```sh
dotnet pack src/studio/framework/Elsa.Studio.Core/Elsa.Studio.Core.csproj \
  --configuration Release -p:IsPackable=true \
  -p:PackageVersion=0.0.0-proof.local.1 \
  --output /tmp/studio-core-proof
python3 scripts/integration-program/verify_studio_package_provenance.py \
  --package-dir /tmp/studio-core-proof \
  --version 0.0.0-proof.local.1 \
  --commit "$(git rev-parse HEAD)"
```

Omitting `--sourcelink-tool` checks archive metadata, icon and assets only. The hosted workflow supplies the pinned tool and checks URL/content against the pushed exact commit. Attach the hosted run, artifact SHA-256 values, and exact head to task #8443 after it completes. Neither this proof nor the shared icon establishes a complete Studio release train, clean installed-package consumer, published package, or production cutover. Retained icon ledger rows should move to completed only after the reviewed proof is merged and its merge commit is recorded.
