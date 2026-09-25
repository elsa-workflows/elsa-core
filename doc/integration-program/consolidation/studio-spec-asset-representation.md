# Retained Studio specification tooling

Program #8194; story #8286. This decision covers the 50 retained Studio
`studio_agent_specification_tooling` assets in the frozen
[legacy-asset ledger](legacy-asset-dispositions.md). It compares the Studio
`20ceaee` Git blobs in the pinned E96 history-import receipt with the active
Core-root files in this change. The [machine-readable decision](studio-spec-asset-representation.json)
and [auditor](../../../scripts/integration-program/audit_current_tip_legacy_assets.py)
fail if source pins, paths, active files, or the classification drift.

**42 files are byte-identical.** Their active representation is the Core-root
file at the same relative path. The mapped Studio `.source` copy is retained
as inert provenance and must not be installed as a second Spec Kit command,
skill, template, or workflow. This is an explicit disposition for those 42
assets; it does not discard Studio source history.

Eight files differ and remain pending individual decisions:

| Difference | Review needed |
| --- | --- |
| `speckit-git-feature` skill and `speckit.git.feature.md` command | Reconcile the Studio command spelling with the active Core command. |
| `speckit-plan` skill | Studio kept `AGENTS.md` feature-neutral; the Core variant updates its plan marker. Review that policy before choosing a single instruction. |
| `.specify/extensions/.registry`, `codex.manifest.json`, `speckit.manifest.json`, `workflow-registry.json` | Check registrations, generated metadata, and ordering without overwriting the active Core installation. |
| `.specify/memory/constitution.md` | Preserve Studio-specific module, backend-awareness, and UX rules in a reviewed Studio-scoped location before retiring the historical copy. |

Run `python3 scripts/integration-program/audit_current_tip_legacy_assets.py` and
`python3 -m unittest discover -s scripts/integration-program -p 'test_current_tip_legacy_assets.py'`
to verify the classification. The broader 163-row ledger remains frozen; this
supplement does not classify the 47 legacy Secrets collision sources or
authorize their activation, package publication, or repository retirement.
