# Shared Extensions and Studio package icon

Program #8194; Story #8286. The Extensions and Studio icon assets retained
under `doc/integration-program/legacy/` are byte-identical to the active Core
root [`icon.png`](../../../icon.png): Git blob
`47e1cadea43af0b7d1a5c489e0d46cf72ecf87eb`, mode `100644`, SHA-256
`82fd76d734d59efc6132af0b0b999146254fa5a296ea5d64f85597bb1cda524e`.
Their historical source pins remain in the 163-row ledger. No duplicate icon
is installed in an active package tree.

The reviewed [Studio package-provenance change](https://github.com/elsa-workflows/elsa-core/pull/8444)
merged into draft history import #8409 as
`530d9489e49a45c66e6922d6fcf597dade0aa5cd`. Its artifact-only workflow
checks the shared icon as well as the Studio package ID, source commit,
framework assemblies and PDBs. On the refreshed import head
`9756691a35ebd538fb0a03d700be8d445f5e44ec`,
[hosted Studio run 36156618949](https://github.com/elsa-workflows/elsa-core/actions/runs/36156618949)
passed. The downloaded `Elsa.Studio.Core` nupkg (SHA-256
`e14176e81a137148a957fb8756d8e85589a34f6b0cde5b81339d8207ed634c79`)
contains one `icon.png` with the exact SHA-256 above.

The stacked [Slack artifact-only proof](https://github.com/elsa-workflows/elsa-core/pull/8436)
at `00d0457cd77fb6a776b44715ec05c44bb2db44e6` also passed in
[hosted run 36156777594](https://github.com/elsa-workflows/elsa-core/actions/runs/36156777594).
Its downloaded `Elsa.Slack` nupkg (SHA-256
`64dd61be37e0c4d843a4459938d97c6ce3b4458c9dce6fc7e77ceab8318d46fa`)
contains one `icon.png` with the same bytes. This verifies package content
for one package from each imported product family; it does not authorize a
publisher cutover or prove every package artifact. The canonical root icon
represents both retained assets, and the `.source` copies remain provenance
until final import review.
