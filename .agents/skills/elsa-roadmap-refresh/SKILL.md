---
name: elsa-roadmap-refresh
description: Refresh the Elsa roadmap from current evidence. Use when Codex needs to update ROADMAP.md or the public roadmap issue by researching elsa-core, elsa-studio, and elsa-extensions source code, specs, releases, commits, issues, pull requests, and discussions; classify shipped foundations, partial/productization work, roadmap candidates, stale items, and recommendations; then update the roadmap checklist and GitHub issue coherently.
---

# Elsa Roadmap Refresh

## Purpose

Use this skill to refresh Elsa's roadmap from evidence across the Elsa product surface:

- `elsa-workflows/elsa-core`
- `elsa-workflows/elsa-studio`
- `elsa-workflows/elsa-extensions`

The output should be a coherent product roadmap, not a raw issue digest. Ground every substantial change in source code, specs, releases, issues, pull requests, or discussions.

## Safety Rules

- Before editing, run `git status --short` in `elsa-core`.
- Never revert unrelated user changes.
- If `ROADMAP.md` or `README.md` already has uncommitted user edits, stop and report the conflict unless the user explicitly asked to work through it.
- Edit only roadmap-related files unless the user asks for more.
- Do not commit, push, or publish unless the user or automation prompt explicitly asks for it.
- If updating GitHub issue `elsa-core#3232`, use the final `ROADMAP.md` body as the issue body.

## Evidence Workflow

1. Read the current roadmap.
   - `ROADMAP.md`
   - `README.md` roadmap link, if relevant
   - GitHub issue `elsa-workflows/elsa-core#3232`

2. Inspect `elsa-core`.
   - Source modules under `src/modules`
   - App hosts under `src/apps`
   - Specs under `specs`
   - Docs under `doc` and `design`
   - Recent releases, commits, open issues, discussions, and open PRs

3. Inspect `elsa-studio`.
   - Source layout and feature modules
   - Designer, diagnostics, auth, localization, embedding, workflow authoring, and instance UX
   - Recent releases, commits, open issues, discussions, and open PRs

4. Inspect `elsa-extensions`.
   - Module families and integration foundations
   - Connections, Secrets, Agents, OpenAPI, schedulers, messaging, persistence, data tooling, and package manifests
   - Recent releases, commits, open issues, and open PRs
   - Discussions if enabled; otherwise state that issue/PR signal is the available source

Use `gh` for GitHub data when available. Prefer local clones when present; otherwise use temporary clones or `gh repo clone` into `/tmp`.

## Classification Rules

Use these categories consistently:

- `[x]` shipped foundation: implemented in source or released enough to build on.
- `[~]` partially shipped or needs productization: code exists, but docs, Studio UX, integration, tests, release packaging, or operational readiness are incomplete.
- `[ ]` roadmap candidate: valuable capability not yet implemented or only represented by issue/PR/proposal.
- stale/obsolete: roadmap item is already done, superseded, or no longer matches repository direction.

Be explicit when a conclusion is an inference from evidence rather than a direct source fact.

## Roadmap Editing Rules

- Keep `ROADMAP.md` product-oriented and exciting, but technically defensible.
- Preserve the main structure unless a better structure is clearly needed:
  - North Star
  - Capability Checklist
  - Current Foundations
  - Production Confidence
  - Authoring Productivity
  - Integrations And Ecosystem
  - Observability And Operations
  - Security, Identity, And Enterprise Readiness
  - AI-Assisted Workflow Engineering
  - Recommended Sequencing
  - Maintainership Recommendations
- Update `Last refreshed` to the current date.
- Keep issue and PR links close to the claims they support.
- Prefer platform tracks over long lists of unrelated features.
- Separate what is already present from what still needs productization.
- Do not overpromise release dates.

## High-Value Themes To Re-Evaluate Each Refresh

- Runtime recovery, graceful shutdown, distributed execution, scheduler/message reliability
- Studio designer reliability, state machine authoring, debugging, progress/timeline, embedded components
- Studio diagnostics, structured logs, console logs, OpenTelemetry, workflow incident timelines
- Extension Platform, package manifests, Connections, Secrets, generated activities, connector SDK
- Agents, MCP/tooling, provider matrix, AI-assisted authoring
- OpenAPI activity provider, Azure Functions, Azure DevOps, Microsoft 365 and Google Workspace integrations
- Security, OIDC, tenant/role activity visibility, localization, white-label readiness
- Marketplace/plugin installation path and package maturity states

## Recommended GitHub Commands

```bash
gh issue view 3232 --repo elsa-workflows/elsa-core --json title,body,updatedAt,url
gh issue list --repo elsa-workflows/elsa-core --state open --limit 200 --json number,title,labels,updatedAt,url
gh issue list --repo elsa-workflows/elsa-studio --state open --limit 200 --json number,title,labels,updatedAt,url
gh issue list --repo elsa-workflows/elsa-extensions --state open --limit 200 --json number,title,labels,updatedAt,url
gh pr list --repo elsa-workflows/elsa-core --state open --limit 100 --json number,title,updatedAt,url
gh pr list --repo elsa-workflows/elsa-studio --state open --limit 100 --json number,title,updatedAt,url
gh pr list --repo elsa-workflows/elsa-extensions --state open --limit 100 --json number,title,updatedAt,url
gh release list --repo elsa-workflows/elsa-core --limit 10
gh release list --repo elsa-workflows/elsa-studio --limit 10
gh release list --repo elsa-workflows/elsa-extensions --limit 10
```

## Publishing

When asked to mirror the roadmap to GitHub:

```bash
gh issue edit 3232 --repo elsa-workflows/elsa-core --body-file ROADMAP.md
```

After publishing, verify:

```bash
gh issue view 3232 --repo elsa-workflows/elsa-core --json title,url,updatedAt
```

Final response should summarize changed themes, whether the GitHub issue was updated, and any files left modified.
