# Templates contributor guidance

Follow the root [AGENTS.md](../AGENTS.md) and [Templates guide](docs/README.md).

- This is the Elsa 3 .NET project-template product: `Elsa.Templates`, `elsa-server`, `elsa-studio`, `elsa-combined`. Preserve independent package versions and released-package consumption. Elsa 4 starters are a separate program.
- Build `templates/Elsa.Templates.slnx` or root `Elsa.Templates.slnf`. The filter selects the product solution, with project paths relative to `templates/`. Root `Elsa.sln` has navigation items only; do not add the pack, tests, embedded starter projects or standalone legacy host to its build/release inventory.
- Keep local props/CPM/targets/NuGet boundaries. The scoped NuGet config selects official packages only. Do not import root source/proof targets or add source project references.
- Generated projects must use a real system-temp workspace outside this checkout, each with its isolated template hive and deterministic async disposal. The smoke fixture packs the actual independent package, checks Core repository/commit provenance, installs, generates and builds all 20 cases.
- The explicit current build/data inventory lives in `scripts/solution/solution-groups.json`; reconcile all tracked project additions there. Six embedded starter `.csproj` files are template content. The standalone root host and lowercase legacy solution are retained original assets, outside supported product build commands.
- Preserve the immutable original import ledger in [source provenance](../docs/integration-program/templates-source-import.json). Normal reviewed source edits do not rewrite donor blobs/history/ref mappings.
- Nested `templates/.github/` jobs are retained inert donor metadata. Source validation grants no publisher, trust, upload, updater, source retirement or archival authority. Release work belongs to #8741/#8725.
