# Contributing to Elsa

Thank you for your interest in contributing to Elsa.

We value clarity, discipline, and maintainability in our codebase. Well-scoped pull requests and high-quality bug reports help us maintain velocity without sacrificing quality.

Whether you're:
- Reporting a bug
- Proposing a feature
- Submitting a fix
- Improving documentation
- Becoming a maintainer

— your contributions are welcome.

---

## Development Workflow

Elsa follows **Trunk Based Development**.

All changes happen through Pull Requests targeting the `main` branch.

1. Fork the repository and create your branch from `main`.
2. If you've added code that should be tested, add tests.
3. If you've changed APIs, update documentation.
4. Ensure the test suite passes.
5. Open a Pull Request.

---

## Pull Requests

We aim for PRs that are easy to review, easy to reason about, and safe to merge.

### 1) One PR = One Concern

Keep PRs focused on a single logical change:

- ✅ Bug fix
- ✅ Refactor (no behavior change)
- ✅ Formatting
- ✅ Dependency update
- ✅ Documentation change
- ❌ Mixing unrelated cleanups with functional changes

**Why:** Mixed concerns increase cognitive load, slow reviews, and increase merge risk.

If you notice cleanup opportunities while fixing a bug:
- Prefer a follow-up PR titled `refactor: ...` or `chore: ...`
- Or keep cleanup strictly limited to what is required for the fix

PRs that mix unrelated concerns may be asked to split before review continues.

---

### 2) Make Reviews Fast

A good PR includes:

- A clear problem statement
- Expected behavior
- Steps to reproduce (if applicable)
- Steps to verify the change
- Screenshots or video for UI changes (if applicable)

The easier your PR is to review, the faster it can be merged.

---

### 3) Prefer Small PRs

Smaller PRs are:
- Easier to review
- Less risky to merge
- More likely to receive timely feedback

If a change is large, consider splitting it into incremental PRs.

---

## Reporting Bugs

We use GitHub Issues to track bugs and feature requests.

When reporting a bug, please include:

- A clear summary
- Steps to reproduce
- Expected behavior
- Actual behavior
- Relevant logs or screenshots
- Sample code (if applicable)

Thorough bug reports significantly increase the chance of a fast and accurate resolution.

---

## Feature Requests & Prioritization

Elsa Workflows is an open source project, and as such, not all feature requests can be implemented immediately or on demand.

Feature development is prioritized based on a combination of the following factors:

### 1. Project Vision & Roadmap

Some features align closely with the long-term direction of Elsa and are prioritized accordingly.

### 2. Real-World Demand

Features that are backed by concrete use cases, especially in production environments, are more likely to be prioritized.

If a feature is important for your scenario, providing context about how you plan to use it is extremely helpful.

### 3. Community Contributions

Contributions from the community are strongly encouraged.

If you are able to propose a design or submit a pull request, it can significantly accelerate progress. Maintainers are happy to collaborate and review contributions.

### 4. Available Time & Maintainer Bandwidth

Elsa is actively maintained, but development capacity is limited. As a result, some features may take time to be implemented.

### 5. Sponsored / Paid Work

Organizations that require specific features or timelines can engage through commercial support.

This allows dedicated time to be allocated to specific work items and can accelerate development significantly.

---

### What This Means in Practice

- There are no guaranteed timelines for feature requests
- Some features may remain open until one of the above factors changes
- The best way to move a feature forward is to:
  - Contribute a proposal or implementation
  - Provide a strong real-world use case
  - Sponsor or fund the work

---

We appreciate all feature requests and feedback — they help shape the direction of Elsa Workflows.

---

## License

By contributing, you agree that your contributions will be licensed under the project's MIT License.
