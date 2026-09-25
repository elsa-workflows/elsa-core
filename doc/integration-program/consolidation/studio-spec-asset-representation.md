# Retained Studio specification tooling

Program #8194; story #8286. This decision covers the 50 retained Studio
`studio_agent_specification_tooling` assets in the frozen
[legacy-asset ledger](legacy-asset-dispositions.md). It compares the Studio
`20ceaee` Git blobs and modes in the pinned E96 history-import receipt with
the active Core-root files in this change. The [machine-readable decision](studio-spec-asset-representation.json)
and [auditor](../../../scripts/integration-program/audit_current_tip_legacy_assets.py)
fail if source pins, paths, active files, or the classification drift.
Studio `main` advanced to `f0eeb3c` without changing any `.agents/` or
`.specify/` file from the pinned `20ceaee` source; repeat that comparison on
the final import tip. The scoped guidance blob in the decision is the active
`src/studio/AGENTS.md` at Core candidate `aad01ec`.

**42 files have identical bytes and Git modes.** Their active representation is the Core-root
file at the same relative path. The mapped Studio `.source` copy is retained
as inert provenance and must not be installed as a second Spec Kit command,
skill, template, or workflow. This is an explicit disposition for those 42
assets; it does not discard Studio source history.

Eight files differ. Six installation/command differences have reviewed
[dispositions](studio-spec-metadata-dispositions.md); the two policy differences
now have [scoped Studio guidance](studio-scoped-policy.md):

| Difference | Active representation |
| --- | --- |
| `speckit-git-feature` skill and `speckit.git.feature.md` command | Use Core's installed command spelling and retain the template placeholder. |
| `speckit-plan` skill | Core keeps its root plan marker behavior; active `src/studio/AGENTS.md` keeps Studio-only plans feature-scoped. |
| `.specify/extensions/.registry`, `codex.manifest.json`, `speckit.manifest.json`, `workflow-registry.json` | Use the active Core installation; the reviewed differences are one extra registration, timestamps and JSON map order. |
| `.specify/memory/constitution.md` | Studio module, backend-awareness and UX rules are represented in active `src/studio/AGENTS.md`; Core's root constitution remains authoritative outside Studio. |

Run `python3 scripts/integration-program/audit_current_tip_legacy_assets.py` and
`python3 -m unittest discover -s scripts/integration-program -p 'test_current_tip_legacy_assets.py'`
to verify the classification. All 50 Studio specification-tooling assets now
have reviewed active representations, subject to a final import-head recheck.
The broader 163-row ledger remains frozen; this
supplement does not classify the 47 legacy Secrets collision sources or
authorize their activation, package publication, or repository retirement.
