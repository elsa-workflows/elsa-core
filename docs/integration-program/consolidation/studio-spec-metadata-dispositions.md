# Studio specification tooling differences

Program #8194; story #8286. The [Studio tooling comparison](studio-spec-asset-representation.md)
pins the source to Studio `20ceaee` and records 42 byte-and-mode identical
files. This review decides six of its eight non-identical files. The active
Core-root installation remains the sole executable Spec Kit installation;
the mapped Studio `.source` copies remain inert history.

| Studio source path | Disposition and evidence |
| --- | --- |
| `.agents/skills/speckit-git-feature/SKILL.md` | Use Core's `/speckit-specify` command spelling. The active `.agents/skills/speckit-specify/SKILL.md` advertises that spelling; Studio's `/speckit.specify` is a stale reference. |
| `.specify/extensions/git/commands/speckit.git.feature.md` | Keep Core's `__SPECKIT_COMMAND_SPECIFY__` template token. This is the extension's unexpanded command template, whereas the installed skill above contains the concrete command name. |
| `.specify/extensions/.registry` | Keep Core's registration. Both registries name the same Git extension version, manifest hash, priority and command set. Core additionally registers the same commands for Claude. The installation timestamp identifies its own installation and is not a runtime contract. |
| `.specify/integrations/codex.manifest.json` | Keep Core's manifest. Integration, version and the complete file-to-hash map are equal after JSON parsing; only `installed_at` differs. |
| `.specify/integrations/speckit.manifest.json` | Keep Core's manifest. Integration, version and the complete file-to-hash map are equal after JSON parsing; only `installed_at` and map insertion order differ. |
| `.specify/workflows/workflow-registry.json` | Keep Core's registry. The workflow name, version, description and source are equal; only installation/update timestamps differ. |

These are decisions about active tool installation, not byte equivalence. The
first comparison's eight-file difference list remains an accurate byte and
Git-mode report. The two policy differences were subsequently reconciled by
the reviewed [Studio-scoped policy](studio-scoped-policy.md):

- `.agents/skills/speckit-plan/SKILL.md`: Core keeps its root Spec Kit plan
  marker behavior; `src/studio/AGENTS.md` keeps Studio-only plans within their
  feature scope without rewriting the root pointer.
- `.specify/memory/constitution.md`: Studio's modularity,
  backend-capability, UX, async/disposal and verification rules are represented
  in active `src/studio/AGENTS.md`; Core's root constitution remains unchanged.

The comparison used the exact `20ceaee` Studio Git blobs and the Core files
in the prepared import at `fb68c8e`, then confirmed the six active Core-root
files at Core `6c9c0532`. The current scoped guidance is pinned separately by
the machine-readable decision. Recheck these decisions if the upstream source tip
or active Core files change before the import merges. This review does not
approve repository retirement, Secrets collision activation, or publication.
