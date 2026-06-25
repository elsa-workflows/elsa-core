# Elsa 3.6.3 Announcement Pack

## Facts

- Product: Elsa Workflows across Elsa Core, Elsa Studio, and Elsa Extensions.
- Version: 3.6.3.
- Release kind: stable patch release.
- Publishing mode: draft-only until approved.
- GitHub releases:
  - Elsa Core: https://github.com/elsa-workflows/elsa-core/releases/tag/3.6.3
  - Elsa Studio: https://github.com/elsa-workflows/elsa-studio/releases/tag/3.6.3
  - Elsa Extensions: https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.6.3
- Package availability verified on NuGet flat-container metadata for representative packages:
  - Elsa 3.6.3.
  - Elsa.Api.Client 3.6.3.
  - Elsa.Studio.Core 3.6.3.
  - Elsa.Scheduling.Quartz 3.6.3.
- No intentional breaking changes are called out in the three release notes.

## Discord

:rocket: **Elsa Workflows 3.6.3 is here!**

We've published the stable **Elsa 3.6.3** patch release across **Elsa Core**, **Elsa Studio**, and **Elsa Extensions**.

:point_right: Core: <https://github.com/elsa-workflows/elsa-core/releases/tag/3.6.3>
:point_right: Studio: <https://github.com/elsa-workflows/elsa-studio/releases/tag/3.6.3>
:point_right: Extensions: <https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.6.3>

Packages are available on NuGet.

### :sparkles: Highlights

:tools: **Azure Service Bus startup stability**
Core now supports an opt-in stable application instance name provider so clustered hosts can reuse Azure Service Bus transport identities across restarts instead of accumulating new subscriptions. Long configured instance names are shortened deterministically, which helps with Kubernetes pod names and Azure Service Bus entity-name limits.

:clock3: **Blank Cron expressions behave as disabled again**
Blank or whitespace Cron values no longer block workflow publishing and no longer throw when a Cron activity runs inside a workflow. Invalid nonblank Cron expressions still fail validation.

:memo: **Better workflow publish feedback**
Publish, bulk-publish, and save-and-publish responses now honor failed publish results and surface validation warnings or errors so clients can show actionable feedback instead of reporting silent success.

:gear: **Quartz and MassTransit extension fixes**
Extensions now ensure durable Quartz jobs exist before scheduling triggers, and MassTransit endpoint suffixes were shortened to reduce Azure Service Bus subscription-name pressure.

:jigsaw: **Studio package alignment**
Studio now consumes `Elsa.Api.Client` 3.6.3, keeping the Studio packages aligned with the Core 3.6.3 patch release.

### :tools: Upgrade notes

There are no intentional breaking changes in this patch release. If you run clustered Elsa hosts on Azure Service Bus, review the stable application instance name option and configure a stable unique name per process or pod where appropriate.

### :raised_hands: Feedback welcome

Please report upgrade issues, regressions, or notes from production-like environments so we can keep the 3.6 line solid.

## LinkedIn

Elsa Workflows 3.6.3 is now available.

This is a stable patch release across Elsa Core, Elsa Studio, and Elsa Extensions, focused on reliability fixes for production operators and cleaner feedback for workflow publishing.

The main improvements are around clustered hosting and scheduling:

- Azure Service Bus hosts can now opt into stable application instance names, helping avoid subscription buildup across restarts.
- Long configured instance names are shortened deterministically, which is useful for Kubernetes and transport entity-name limits.
- Blank Cron expressions are treated as disabled again, restoring a smoother workflow publishing experience while still rejecting invalid Cron values.
- Workflow publish APIs now return clearer validation warnings and errors.
- Extensions include Quartz durable trigger scheduling fixes and shorter MassTransit endpoint names.
- Studio has been aligned with the Elsa Core 3.6.3 client packages.

No intentional breaking changes are included.

Release notes:
https://github.com/elsa-workflows/elsa-core/releases/tag/3.6.3
https://github.com/elsa-workflows/elsa-studio/releases/tag/3.6.3
https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.6.3

## X Single-Post Option

Elsa Workflows 3.6.3 is available across Core, Studio, and Extensions.

This stable patch release improves Azure Service Bus clustered hosting, blank Cron handling, publish validation feedback, Quartz durable trigger scheduling, and Studio package alignment.

Core notes: https://github.com/elsa-workflows/elsa-core/releases/tag/3.6.3

## X Thread Option

1/ Elsa Workflows 3.6.3 is available across Core, Studio, and Extensions.

This stable patch release focuses on reliability: Azure Service Bus clustered hosting, Cron publishing behavior, publish validation responses, Quartz scheduling, and package alignment.

Core notes: https://github.com/elsa-workflows/elsa-core/releases/tag/3.6.3

2/ Highlights:

- Stable application instance names for clustered Azure Service Bus hosts.
- Deterministic shortening for long configured instance names.
- Blank Cron expressions behave as disabled again.
- Publish APIs surface validation warnings and errors.

3/ Extensions add Quartz durable trigger scheduling fixes and shorter MassTransit endpoint names.

Studio is aligned with `Elsa.Api.Client` 3.6.3.

No intentional breaking changes.

Studio: https://github.com/elsa-workflows/elsa-studio/releases/tag/3.6.3
Extensions: https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.6.3

## Links

- Elsa Core release: https://github.com/elsa-workflows/elsa-core/releases/tag/3.6.3
- Elsa Studio release: https://github.com/elsa-workflows/elsa-studio/releases/tag/3.6.3
- Elsa Extensions release: https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.6.3
- NuGet package search: https://www.nuget.org/packages?q=Elsa

<!-- Review and approve exact copy before publishing. -->
