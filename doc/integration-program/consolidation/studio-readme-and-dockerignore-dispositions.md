# Studio README and Docker context dispositions

Program #8194; story #8286; draft import #8409. This note resolves the Studio
root README and `.dockerignore` rows in the
[retained-asset ledger](legacy-asset-dispositions.md). The frozen source bytes
remain under `doc/integration-program/legacy/studio/` for provenance.

| Studio source path | Pinned blob and mode | Active Core representation |
| --- | --- | --- |
| `README.md` | `d1b23bc8d67e931e311c43d06a9a0b10b7cde101`, `100644` | [Studio source and local development guide](../../studio/README.md), blob `1bcbeee61947bc6b56b79a5d8a9b97d9a83252e7`, `100644`; introduced in draft-import child PR [#8459](https://github.com/elsa-workflows/elsa-core/pull/8459), merge commit `c01f9c53f07a3db6ec48b15ea97e784272363df9`. |
| `.dockerignore` | `38bece4e1ed9968d70beb5815ba4dcead8b592d5`, `100644` | Root [`.dockerignore`](../../../.dockerignore), blob `9fd202e8fac99839317ea15a5b6a9fe600081ab6`, `100644`; introduced by [#8461](https://github.com/elsa-workflows/elsa-core/pull/8461), merge commit `eab0bbf825ff42f4e7ba344dfdf0ae2fd9f094c0`. |

The initial source-tip verification was at Studio main
`099402226daba80e473a306bbd1243b8994465b8`. Current Studio main has advanced to
`5b34ec327caffd132e18bfd88bfc9e862dc35c43`; both commits contain the same
recorded README and `.dockerignore` blobs shown above. The earlier refresh at
`0994022` changed only the React sample import and OpenTelemetry test comments.
The inert source copies match those exact blob identities in the current draft
import.

The standalone Studio README directs contributors to clone `elsa-studio`, open
`Elsa.Studio.sln`, and build projects under the old `src/framework` layout. The
active `doc/studio/README.md` replaces those instructions with the canonical
`Elsa.sln`, the Node 22 ClientLib build script, and current `src/studio` host
paths. The Core root README links to this Studio-specific guide. Historical
badges and the original Weblate status widget remain in the inert source copy;
they are not presented as current consolidated-build or localization status.

The old `.dockerignore` was the root policy for the separate Studio repository.
In the consolidated tree, current image builds use the repository root as their
context: `docker-ca.yml` invokes `docker build … .`, and the Docker Compose
configuration uses `context: ../.`. The root `.dockerignore` is the active
policy for that context. It excludes generated output and developer-local or
sensitive files while preserving root `README.md`, `LICENSE`, and Dockerfile
inputs. Copying the Studio rules wholesale would add root exclusions for those
files and would not create a scoped Studio context. No current build invocation
uses the old standalone Studio context. The source rules therefore map to the
expanded consolidated root policy, not to a second active `.dockerignore`.

Reproduce the pinned and active identities with:

```sh
git ls-tree 099402226daba80e473a306bbd1243b8994465b8 README.md .dockerignore
git ls-tree 5b34ec327caffd132e18bfd88bfc9e862dc35c43 README.md .dockerignore
git ls-tree HEAD doc/studio/README.md .dockerignore \
  doc/integration-program/legacy/studio/README.md.source \
  doc/integration-program/legacy/studio/.dockerignore.source
```

These are path-specific asset dispositions for the current draft tree. They do
not authorize merging the history import, removing provenance copies, changing
publishers, or claiming final import acceptance.
