# Consolidated Copilot setup for imported modules

Program #8194; asset classification #8286. The archived Extensions and Studio
`.github/workflows/copilot-setup-steps.yml.yml.source` files remain inert
provenance. Their pinned blobs are `d76e7229e523afa8da884cfe7003a72999e7f128`
(Extensions at `33fa0bfd28c7585240e3d4f665058c067b17e287`) and
`f5f80a5d39a33f5cc3cb7bf84a29e40af01c1e7c` (Studio at
`9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`). Both archived workflows
install only .NET 9.

Core already owns the active `.github/workflows/copilot-setup-steps.yml`.
At draft import `aefa5bfd4fe43ecd695f0953d179187ab00e3268`, its blob is
`9070ba5c47ace810172ecc836fc998442ed284f4` and it installs .NET 10.
A bounded local SDK 10.0.300 check on that exact source restored and built
`Elsa.Email`, `Elsa.Studio.Core.BlazorServer`, and
`Elsa.Studio.Workflows.Designer` for net8.0, net9.0, and net10.0. That check
does not stand in for the full solution CI or a Copilot agent session.

Studio ClientLib work also needs Node 22. The active
[Studio ClientLib workflow](../../../.github/workflows/studio-clientlib-build.yml)
uses Node 22 and .NET 10; its [run 36189257282](https://github.com/elsa-workflows/elsa-core/actions/runs/36189257282)
passed on the same draft import. The root Copilot setup did not provide Node.
Reviewed [PR #8475](https://github.com/elsa-workflows/elsa-core/pull/8475)
added the same pinned `actions/setup-node` action and Node 22 version used by
the ClientLib workflow. Its exact-head
[Copilot setup run 36191113978](https://github.com/elsa-workflows/elsa-core/actions/runs/36191113978)
passed both .NET 10 and Node 22 setup steps at
`fa262bbeca338011d509413341a32aaad392c857`. PR checks and Greptile 5/5
passed before merge `af0f659657e91fd4f48ecf3e8c5e870cb0ea05d4`.
This is one setup contract for the combined source tree, without activating
either archived workflow or a package publisher.

The two asset-ledger rows now cite that reviewed PR, its merge commit, and
the active workflow blob `281318c650bf14f5353f4783a555dea3a5e26edc`.
The Extensions row is represented by the root .NET setup; the Studio row also
has direct Node 22 setup evidence. A passing setup job does not prove an
end-to-end Copilot agent session, complete solution CI, or package publication.
