# Identical Studio agent and prompt assets

Program #8194; Story #8286. Eleven Studio source files already exist at
their active Core paths with the same Git blob and `100644` mode. This
compares the pinned Studio source `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`,
its inert `.source` copies in import draft #8409 at
`530d9489e49a45c66e6922d6fcf597dade0aa5cd`, and Core `main` at
`34de0aa24bb785cd0b0ed4d5a237efb19dda2122`.

| Active Core path and retained Studio source path | Git blob |
| --- | --- |
| [`.github/agents/release-notes.agent.md`](../../../.github/agents/release-notes.agent.md) | `9c91f9e69c6c224629fa6a893d34bcde6d57a36a` |
| [`.github/agents/speckit.git.commit.agent.md`](../../../.github/agents/speckit.git.commit.agent.md) | `c7de63ee014d61b2e3a74b92edffdc3343142889` |
| [`.github/agents/speckit.git.feature.agent.md`](../../../.github/agents/speckit.git.feature.agent.md) | `080b52c9a65451699438775e87305e9448a2eaae` |
| [`.github/agents/speckit.git.initialize.agent.md`](../../../.github/agents/speckit.git.initialize.agent.md) | `f7522c105a88c6445babc7fe55497cb70e99b8e4` |
| [`.github/agents/speckit.git.remote.agent.md`](../../../.github/agents/speckit.git.remote.agent.md) | `b8f0fb8b8e9ef20f4e91c001d2aba1a0e9aeffec` |
| [`.github/agents/speckit.git.validate.agent.md`](../../../.github/agents/speckit.git.validate.agent.md) | `dfd751f2af0cb09ae74a5dd52c13b6141f55e5da` |
| [`.github/prompts/speckit.git.commit.prompt.md`](../../../.github/prompts/speckit.git.commit.prompt.md) | `d87e3dfb942e4e56323c584499634e4048e70035` |
| [`.github/prompts/speckit.git.feature.prompt.md`](../../../.github/prompts/speckit.git.feature.prompt.md) | `91ae09b1619897c0dcafd7caad40eda29cd5d4c8` |
| [`.github/prompts/speckit.git.initialize.prompt.md`](../../../.github/prompts/speckit.git.initialize.prompt.md) | `02c279cd5ada9636469f26c7a95f3d5c9b99cb0d` |
| [`.github/prompts/speckit.git.remote.prompt.md`](../../../.github/prompts/speckit.git.remote.prompt.md) | `a521d9de3d40f3c373e8dd40ae81c8aa49bc59ff` |
| [`.github/prompts/speckit.git.validate.prompt.md`](../../../.github/prompts/speckit.git.validate.prompt.md) | `18ac70feba448a25fbfa373bc3b19d84fdf939c0` |

The five Spec Kit agent/prompt pairs and the Release Notes agent need no
duplicate active files. This is a source-representation decision: it does not
prove an external Copilot activation, auto-commit operation, or generated
release-note quality. The separate Extensions release-notes agent and
playbook, Studio Copilot instructions/playbook, and Studio `AGENTS.md` have
different bytes and remain pending their own review. The 50 Studio Spec Kit
tooling assets have a separate [reviewed disposition](studio-spec-asset-representation.md).

Reproduce in the checked-out import draft with `git ls-tree HEAD` on each
active path and its `doc/integration-program/legacy/studio/` `.source`
counterpart. Each pair has the blob above and `100644` mode. Core `main`
has the same active identities. No agent, prompt, build, package, or
publication file changes in this decision.
