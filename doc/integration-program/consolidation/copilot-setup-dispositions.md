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
does not stand in for the full solution CI or a Copilot-hosted setup run.

Studio ClientLib work also needs Node 22. The active
[Studio ClientLib workflow](../../../.github/workflows/studio-clientlib-build.yml)
uses Node 22 and .NET 10; its [run 36189257282](https://github.com/elsa-workflows/elsa-core/actions/runs/36189257282)
passed on the same draft import. The root Copilot setup did not provide Node.
The proposed active-workflow change adds the same pinned `actions/setup-node`
action and Node 22 version used by the ClientLib workflow. This is one setup
contract for the combined source tree, without activating either archived
workflow or a package publisher.

The two asset-ledger rows stay pending in this PR. After the change is
reviewed and the active workflow has run on the exact PR branch, record its
final active blob, decision PR and merge commit in a follow-up ledger update.
The Extensions row can then be represented by the root .NET setup; the Studio
row additionally requires the Node 22 setup result. A passing ClientLib CI
job alone is not proof that the Copilot setup job supplies Node.
