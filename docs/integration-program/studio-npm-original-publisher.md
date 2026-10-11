# Studio original npm pair publisher preparation (#8734)

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

The default, unauthenticated operation is:

```sh
python3 scripts/integration-program/publish_studio_npm_pair.py \
  --archive /physical/path/original.zip --packet /physical/path/reviewed.json \
  --packet-sha256 REVIEWED_PACKET_SHA256 --registry feedz \
  --output /physical/path/reconciliation.json
```

A successful verify receipt means local original admission and a complete
matching/missing public registry observation. It does not mean publication,
private-reader rights, clean lifecycle replay, release acceptance or cutover.
The accepted A221 proof is eligible only for this operation. Original consumer
proof is retained and checked; this command does not rerun that consumer.

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
identity. No publish workflow, environment, credential or OIDC authority is
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

WASM is uploaded first. A later failure leaves an honest partial pair. On an
uncertain command result, readback is recorded and execution stops even when the
bytes are visible. Reinvoke with the same original ZIP/packet after reviewing the
receipt; matching members are skipped, and only missing members are considered.
A conflict response is never equivalent to matching bytes. The pair is not an
atomic transaction. Dist-tag state is not independently verified by this version
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

Npm automatic provenance would identify the publisher invocation, rather than
prove the retained original producer run. The native path requires producer and
executor source commits to match; original producer identity remains separately
bound by the receipt/provider readback. The final provenance policy and trusted
publisher configuration still require explicit operational review before activation.
