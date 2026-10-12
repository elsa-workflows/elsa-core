# Studio original npm pair custody and publisher preparation (#8734 / #8736)

The executor consumes the original same-run ZIP. It never builds, repacks, chooses
versions or creates tags. The existing `0.0.0-proof.RUN.ATTEMPT` identity remains
verification-only. A separate producer switch, `--release-candidate`, emits a
schema 2 `release-candidate` identity through the same build, pack and consumer
path. This switch prepares unpublished bytes; it does not reserve a version or
authorize publication. The existing producer workflow offers an explicit main-only manual 3.10 candidate
mode, with proof as the default. No candidate producer has yet been run. The
3.8/3.9 candidate identity grammar preserves future maintenance routes, but their
reviewed producer/provenance admission remains unconfigured.

Create a reviewed JSON packet with exactly these fields:
`source_commit`, `source_tree`, `run_id`, `run_attempt`, `artifact_id`,
`artifact_name`, `archive_sha256`, `receipt_sha256`, `version`. IDs are positive
integers. Digests identify the original provider ZIP and retained receipt. Proof
artifact names remain `studio-npm-pair-COMMIT-RUN-ATTEMPT`; separate candidate
artifacts use `studio-npm-candidate-COMMIT-RUN-ATTEMPT`. The executor re-reads anonymous GitHub artifact/run metadata and requires exact
identity, digest, size, source, attempt, successful producer and unexpired artifact.
A local packet hash alone is not provider authority. Unavailable metadata stops admission.
Use physical paths with no symlink ancestors for original inputs.

The default, unauthenticated local operation is:

```sh
python3 scripts/integration-program/publish_studio_npm_pair.py \
  --archive /physical/path/original.zip --packet /physical/path/reviewed.json \
  --packet-sha256 REVIEWED_PACKET_SHA256 --registry feedz \
  --output /physical/path/reconciliation.json
```

The report destination must be separate from both inputs. Before input admission,
the command rejects direct, resolved-path and hardlink aliases, report symlinks,
and invalid report parent directories with `unsafe_report_destination` on stdout;
it writes no report on that rejection. Safe parent-directory aliases such as
macOS `/tmp` are resolved to their physical destination. Report staging is reserved
before input admission; an unavailable parent fails through the same closed code.
An existing separate regular report may be replaced. Reports are staged beside
the destination and atomically replace its leaf, so a late leaf link cannot truncate an original
input. Report-write failures return sanitized `report_write_failed` on stdout.
Use trusted directories under exclusive local ownership: these pathname checks
and leaf replacement do not protect against adversarial parent-directory races.

A successful verify receipt means local original admission and a complete
matching/missing public registry observation. It does not mean publication,
private-reader rights, clean lifecycle replay, release acceptance or cutover.
The accepted A221 proof is eligible only for this operation. Original consumer
proof is retained and checked; this command does not rerun that consumer.

## Hosted fixed-original custody (#8736)

The manual [Studio original npm pair custody workflow](../../.github/workflows/publish-studio-original-pair.yml)
has retrieval and verification jobs only. Its only input is the exact reviewed
executor head. Dispatch it on `main` after the implementation's exact-head review,
CI and main integration have passed:

```sh
gh workflow run publish-studio-original-pair.yml --ref main \
  -f expected_head=REVIEWED_MAIN_EXECUTOR_SHA
gh run list --workflow publish-studio-original-pair.yml --event workflow_dispatch
gh run watch CUSTODY_RUN_ID --exit-status
gh run download CUSTODY_RUN_ID \
  -n studio-npm-original-evidence-CUSTODY_RUN_ID-CUSTODY_ATTEMPT \
  -D /physical/path/custody-evidence
gh run download CUSTODY_RUN_ID \
  -n studio-npm-original-input-CUSTODY_RUN_ID-CUSTODY_ATTEMPT \
  -D /physical/path/original-input
```

An empty, malformed or wrong head, a non-main ref, missing inputs or unavailable
provider metadata fails the run. The workflow cannot choose another artifact,
version, registry, operation, policy or source. Changed identities require a
reviewed source change. It fixes the nine-field packet from #8736, including
producer S `a221888910e96711d07e66f86f6e698edde6524b`, run `37911724081` attempt `1`,
original artifact `11607565131`, source tree
`0407d373f981c9b29e491b739051e200214682ea` and version
`0.0.0-proof.37911724081.1`. The exact original JSON bytes, including their final
newline, have SHA-256
`0af4dd27274201e2cb6d13d8d4709304cd4e0da2c0603260d3e3355e4c3703f4`.
The original ZIP is 94,760,475 bytes with SHA-256
`3f05853f6fad6fbac7389d9cf12825bc0914e457feb5dd79f57db7914a1d96fe`;
the retained producer receipt has SHA-256
`2957ac31ccedf69e702becd6b714d1a076970dbafd0aa4aebf4bbe6fd1a83280`.

The checkout-free reader has only `actions:read`. It validates the successful,
unexpired original producer and artifact, downloads the provider ZIP as a file,
and checks its size and digest without extracting, repacking or rebuilding it.
The same-run transfer contains `original.zip`, the unchanged `packet.json`, and
allowlisted `artifact.json`, `producer-run.json` and `retrieval.json` records.
Raw provider bodies, signed download URLs and credential values are discarded.
This transfer has a new artifact ID and digest; it is never labeled the original
provider artifact. Both transfer and evidence artifacts are retained for 30 days.
Original-provider availability and expiry remain separate admission requirements.

The separate verifier has `contents:read`, `actions:none`, a checkout pinned to
the workflow source with `persist-credentials:false`, and no artifact-read
GitHub token, publisher credential, protected environment or OIDC grant. Same-run
artifact download uses the Actions artifact service. The custody helper reuses
the existing provider admission and physical-path validators. It independently
checks the clean executor D, native run/attempt/workflow/main identity, exact
transferred packet/ZIP, retained and fresh original provider records, and fresh
same-run transfer identity/digest. D may differ from original producer S.

Both registry observations then use the existing `publish_studio_npm_pair.py`
CLI with its default `verify` operation. npmjs is still observed if Feedz returns
an unknown/error result; either failed observation keeps the run failed. The
workflow also checks source cleanliness and original ZIP/packet hashes after the
observations. `custody.json` records input admission only; `feedz.json` and
`npmjs.json` record the independent registry outcomes. Inspect all three receipts
and the native run conclusion. A successful custody receipt does not turn a
failed registry receipt or failed run into acceptance. An admission failure may
produce only a sanitized custody failure receipt; an earlier reader failure
prints a fixed failure code and produces no transfer.

For independent closeout, read the native run and original/transfer artifact
metadata, compare D/run/attempt and S against `custody.json`, hash the downloaded
`original.zip` and exact `packet.json`, and inspect `receipt.json` inside that ZIP
against the pinned receipt digest. Check both registry receipts for
`success:true`, `publisher_authority:false`, no uploads and
`registry_observation_scope:anonymous_public`. A local verification is not a
hosted proof. The lead owns the single native proof and evidence readback for
#8736; workers do not dispatch it.

This proof is permanently unpublishable. Anonymous public matching/missing
observations establish neither intended-reader access nor maintenance support.
Actual maintenance venues, publisher identity/trust/grants, reader access,
dist-tags, source-workflow retirement, publication, deployment and archival remain
open and separately reviewed. `REVIEWED_PUBLICATION_POLICY` remains `None`.
No second executor or producer is introduced: the additive workflow/helper is
needed to establish the credential-separated hosted custody boundary that the
local executor cannot establish on its own.

Ownership/exit: Task #8736 owns the fixed-original custody workflow and helper.
Retain them as the reviewed original-input stage of the eventual publisher, or
remove this fixed-proof preparation after a reviewed replacement and immutable
original custody/recovery proof. #8734 continues to own the existing executor.

Both members are reconciled before every upload. Matching means downloaded raw
tarball bytes equal the admitted original, with matching metadata identity and
SHA-512 integrity. Feedz version 404 alone remains unknown: its complete public
package document must identify the package and contain a valid versions map
that omits the requested version. Authentication errors, malformed/filtered
metadata, redirects, timeouts, unknown hosts and uncertain bodies stop writes.
The public packument protocol still needs review against intended reader access;
it is not authenticated absence evidence. npmjs 3.10.x remains refused, including
prereleases, matching current Studio source policy. Existing maintenance venue
policy remains separate from current-main runtime compatibility evidence.

The future `--operation publish` path is disabled by
`REVIEWED_PUBLICATION_POLICY = None`. Activation requires a separately reviewed
native workflow with the exact workflow/ref/SHA, environment and protection rule
IDs, current-run approval by a named reviewer other than the triggering actor,
exact original packet/action/registry authorization and verified publisher
identity. No publication job, environment, credential or OIDC authority is
created by this change. Credential namespace isolation and actual grants,
trusted-publisher configuration, the original release producer workflow/artifact
provenance and intended reader access remain activation gates. The current
native environment inventory does not satisfy these gates.

The publisher command accepts only the admitted local tarballs. It requires
Node >=22.14.0 and npm >=11.5.1; the available owned Node22 installation has
npm10.9.9 and cannot serve as the publishing toolchain. Feedz follows the existing
base64-token source convention without interpreting token scope. npmjs uses
trusted-publisher OIDC without inherited npm configuration or token fallback.
Stable candidates use `latest`; prereleases use `next`.

WASM is uploaded first. A fresh pair reconciliation must show matching WASM
immediately before any React write; a missing dependency stops with the partial
receipt retained. A later failure leaves an honest partial pair. On an
uncertain command result, readback is recorded and execution stops even when the
bytes are visible. Reinvoke with the same original ZIP/packet after reviewing the
receipt; matching members are skipped, and only missing members are considered.
A conflict response is never equivalent to matching bytes. The pair is not an
atomic transaction. The remaining read/write race requires operational publisher
serialization and provider immutability controls; this guard provides no atomic
publication guarantee. Dist-tag state is not independently verified by this version
and must not be claimed as a completed release gate.

This work prepares #8220 publication handoff and the npm item in #8725; it does
not replace the other #8725 criteria or authorize activation/archive. #8727 owns
release-skill documentation. Link this command and its exact original identities
from the later approved operational packet before activation.

Primary protocol and policy sources: [Feedz npm](https://feedz.io/docs/package-formats/npm),
[npm registry API](https://github.com/npm/registry/blob/main/docs/REGISTRY-API.md),
[npm publish](https://docs.npmjs.com/cli/v11/commands/npm-publish/),
[npm trusted publishers](https://docs.npmjs.com/trusted-publishers/), and
[current Studio workflow](https://github.com/elsa-workflows/elsa-studio/blob/main/.github/workflows/packages.yml).

Ownership: Task #8734 / Program #8194 owns this cutover preparation. At publisher
cutover, retain it as the owned operational executor or remove the migration
preparation after a reviewed replacement and original-artifact recovery proof.
The #8725 broader cut gate remains unchanged.

The original producer commit S and later publisher executor commit D are
independent. The source-pinned action in D binds the canonical reviewed original
packet digest (including S, producer run/attempt and original ZIP/receipt digests).
GitHub run/workflow/ref metadata and the clean checkout independently verify D;
native approval is for that exact publisher run. D need not equal S, and D never
contains a self-SHA constant. This permits reviewing original bytes before the
later activation change without a circular source/artifact dependency.

Each source-pinned action must also explicitly include
`registry_provenance_policy`: `no-registry-attestation` for Feedz, or
`publisher-invocation` for npmjs. Missing, unknown or wrong policies fail before
publisher access. Automatic npm provenance remains enabled: it describes the
publisher invocation at D, rather than proving that invocation built the
retained original at S. The npmjs policy explicitly accepts this distinction;
it is not an original-producer attestation. The sanitized admission record
retains the canonical packet digest, S, observed D/run/attempt and policy, with
`registry_attestation_verified: false`. This executor does not verify an uploaded
attestation. These are policy and native identity observations, not proof of
registry attestation content.

Publication policy remains unconfigured. Actual trusted-publisher/provenance
configuration still requires operational review. The 3.10 npmjs refusal and the
unconfigured 3.8/3.9 maintenance producer restriction remain in force; a
synthetic policy test does not authorize those routes.
