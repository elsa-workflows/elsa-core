# Extensions NUKE schema and solution selection

Program #8194; Feature #8214; Story #8286. This decision covers two retained
Extensions `.nuke` assets in the [163-asset ledger](legacy-asset-dispositions.md).
The frozen source pin is Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`;
the current Core `main` examined here is `6ae33440214c5926a5bda75d55ec2815a492b9ef`.
The history-import draft #8409 keeps the source files as inert `.source` copies.

| Extensions source | Source Git blob | Active Core file | Decision |
| --- | --- | --- | --- |
| `.nuke/build.schema.json` | `8391009603dad514cbbac678cd26dec287d5b378` | [`.nuke/build.schema.json`](../../../.nuke/build.schema.json), same blob and `100644` mode | One root schema represents the imported copy byte for byte. |
| `.nuke/parameters.json` | `95ed63cfb3bc8768fbecd0d2f42eccf1e5ff4ab8` | [`.nuke/parameters.json`](../../../.nuke/parameters.json), blob `8535901f9e421af9db8c52d6b52f175a0d974d72`, `100644` mode | Retain the parameter shape, but select the canonical `Elsa.sln` instead of the no-longer-active `Elsa.Extensions.sln`. |

The two parameter files differ only in the `Solution` value. The root NUKE
build reads the selected solution through `IHazSolution`; its `TestProjects`
selection enumerates test projects from that solution. Keeping the old
`Elsa.Extensions.sln` value at the root would bypass the consolidated build
and refer to a solution absent from Core. The active root schema is byte
identical to the retained Extensions schema, so there is no second generated
target contract to install.

This representation does not by itself prove that every imported project and
test is included in `Elsa.sln`. That is a separate final import gate in #8409
and #8214. The reviewed source-import draft includes an updated `Elsa.sln`;
its hosted full build and mapped package proof must pass at its final head.
The `.source` copies remain inert provenance until the import is accepted.

Reproduce the comparison from the checked-out import draft with `git ls-tree
HEAD .nuke/build.schema.json .nuke/parameters.json
doc/integration-program/legacy/extensions/.nuke/build.schema.json.source
doc/integration-program/legacy/extensions/.nuke/parameters.json.source`,
`cmp .nuke/build.schema.json
doc/integration-program/legacy/extensions/.nuke/build.schema.json.source`,
and `diff -u doc/integration-program/legacy/extensions/.nuke/parameters.json.source
.nuke/parameters.json`. The source copies' blobs and modes match the frozen
ledger. No NUKE target, publish workflow, package, or production setting is
changed by this decision.
