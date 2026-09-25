# Legacy assets at the E96 import tip

Program #8194; story #8286. The [original 163-row disposition ledger](legacy-asset-dispositions.md) remains a frozen record of Core `076f022`, Extensions `33fa0bf`, and Studio `9afd3e3`. Its validator deliberately rejects a different source tip. This refresh compares it with the [E96 import receipt](current-tip-e96-evidence/import-receipt.json.gz) at Core `e96fd36`, Extensions `ba8b71d`, and Studio `20ceaee`; it does not rewrite the historical record or mark pending dispositions complete.

All **163 retained assets** remain present at the same mapped paths and Git modes: 80 Extensions and 83 Studio. There are no added or missing legacy asset paths. The four changed Git blobs are all from Extensions:

| Retained source path | Change since the frozen ledger | Existing disposition gate |
| --- | --- | --- |
| `.github/workflows/packages.yml` | Publisher base version `3.8.0` to `3.10.0` | Keep inert; one-publisher cutover remains separate. |
| `Directory.Build.props` | Elsa/Studio package references move to the `3.10.0` preview baselines. | Preserve scoped package-version behavior in the active build. |
| `Directory.Packages.props` | Adds `Blazored.FluentValidation` `2.2.0`. | Check the scoped central package graph. |
| `src/modules/secrets/Elsa.Studio.Secrets/Elsa.Studio.Secrets.csproj` | Adds a `Blazored.FluentValidation` package reference. | Secrets compatibility #8275 and the Studio API contract #8301 remain open. |

The source Git diff is eight insertions and three deletions across those four files. The current-tip receipt's uncompressed SHA-256 is `06cd198a338d5c6d49fa6b0183bbda6b602252f39f622f18084e60342880bb75`. In the materialized [draft import PR #8409](https://github.com/elsa-workflows/elsa-core/pull/8409) at `64693216c38207eb47397b9367e57cd2f435fd6c`, the auditor checked the Git blob hash and executable bit of every retained `.source` file against that receipt: **163/163 matched**.

Recheck the committed receipt and ledger with:

```sh
python3 scripts/integration-program/audit_current_tip_legacy_assets.py
```

To also check a materialized import tree, use `--import-root /path/to/import-checkout`. The exact source pins and receipt digest are enforced, and the auditor fails on missing/duplicate assets or changed mapped paths and modes. These checks establish retained-source integrity. They do not classify each asset as integrated or retired, prove package-mode consumers, authorize publishing, or satisfy the Secrets upgrade gate. Before merging the history-bearing import, attach completion evidence to each disposition row or record an explicit, reviewed retirement decision; keep unresolved `.source` copies inert.
