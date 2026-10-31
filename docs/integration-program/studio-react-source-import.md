# React Studio source import

The immutable merge `b452c521f5f1e847094a02aab65edb9950b3460c` maps the complete
donor tree into `studio/react/`. Its second parent is the original donor commit,
so all 14 original commits retain their authors, timestamps and IDs. No squash or
rebase was used. The subtree tree ID equals the source root tree ID.

[The machine-readable receipt](studio-react-source-import.json) owns source identity,
snapshot, refs, licensing observations and cutover state. The remote main tip matched
the snapshot at import time; there were no later commits to port. Adaptations follow
the immutable import in separate commits and do not change this provenance claim.

The task owner explicitly removed the Nexxbiz clearance requirement and authorized
import and adaptation on 2026-10-20. The donor has no license file; this record does
not invent a license grant or alter attribution. Dependency compatibility remains a
separate validation requirement of #8842/#8847.

Run from the repository root:

```sh
python3 scripts/integration-program/verify_studio_react_source_import.py
python3 -m unittest discover -s scripts/integration-program -p 'test_verify_studio_react_source_import.py'
```

The retained guard verifies the original two-parent merge, exact donor tree, change
boundary, original history, source refs and post-snapshot ancestry. It deliberately
permits subsequent application adaptations. The tests exercise valid adaptation and
rejection of changed trees, squashed imports, missing ancestry and source-tip loss.

Archive and redirect the donor after the import is independently reviewed and
merged. Recheck its remote tip at cutover and preserve intervening commits first.
The [architecture decision](../adr/2026-10-20-modular-react-studio-preview.md) records
the bounded spike evidence and remaining production requirements.
