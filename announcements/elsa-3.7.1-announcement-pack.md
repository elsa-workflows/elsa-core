# Elsa 3.7.1 Announcement Pack

## Facts

- Product: Elsa Workflows
- Version: 3.7.1
- Release kind: stable patch release
- Published: June 21, 2026
- Repositories: Elsa Core, Elsa Studio, Elsa Extensions
- GitHub releases are public and not prereleases.
- Representative NuGet packages verified at 3.7.1:
  - Elsa, Elsa.Api.Client, Elsa.Hosting.Management, Elsa.Workflows.Runtime, Elsa.Workflows.Management
  - Elsa.Studio, Elsa.Studio.Core, Elsa.Studio.Workflows, Elsa.Studio.Workflows.Designer, Elsa.Studio.Authentication.ElsaIdentity
  - Elsa.Scheduling.Quartz, Elsa.Caching.Distributed.MassTransit, Elsa.ServiceBus.MassTransit.AzureServiceBus, Elsa.ServiceBus.MassTransit, Elsa.ServiceBus.AzureServiceBus

## Discord

:rocket: **Elsa Workflows 3.7.1 is here!**

We've published the stable **Elsa 3.7.1** patch release across **Elsa Core**, **Elsa Studio**, and **Elsa Extensions**. Packages are available on NuGet.

:point_right: Core: <https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.1>
:point_right: Studio: <https://github.com/elsa-workflows/elsa-studio/releases/tag/3.7.1>
:point_right: Extensions: <https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.7.1>

### :sparkles: Highlights

:cloud: **Azure Service Bus hosting reliability**
Elsa Core now supports stable application instance names so clustered hosts can reuse Azure Service Bus-backed change-token subscriptions and queues across restarts.

:straight_ruler: **Safer Azure Service Bus entity names**
Long configured instance names are shortened deterministically, and Extensions shortens MassTransit endpoint suffixes to reduce Azure Service Bus entity-name pressure.

:clock3: **Quartz scheduling fix**
Extensions now ensures the durable Quartz workflow job exists before scheduling triggers, preventing trigger storage failures.

:link: **Package alignment**
Studio now consumes `Elsa.Api.Client` 3.7.1, and Extensions aligns with Core and Studio 3.7.1 packages.

### :tools: Upgrade notes

No breaking changes are expected. If you run clustered Elsa hosts with Azure Service Bus, configure a stable per-instance name for each concurrently running host. Quartz-backed scheduling deployments should pick up the durable-job fix automatically.

### :raised_hands: Feedback welcome

Please report upgrade issues, regressions, or deployment notes you run into so we can keep improving the 3.7 line.

## LinkedIn

Elsa Workflows 3.7.1 is now available as a stable patch release across Elsa Core, Elsa Studio, and Elsa Extensions.

This release focuses on operational reliability for production deployments:

- Azure Service Bus-backed clustered hosts can now use stable application instance names, reducing restart-driven entity buildup.
- Long configured instance names are shortened deterministically so deployment-provided names can remain stable while fitting Azure Service Bus limits.
- Quartz-backed scheduling now verifies the durable workflow job before scheduling triggers.
- Studio and Extensions packages are aligned with the Elsa 3.7.1 package set.

No breaking changes are expected. If you run Elsa in a clustered Azure Service Bus setup, review the upgrade notes around `ApplicationInstanceOptions.InstanceName` and `ApplicationInstanceOptions.InstanceNameEnvironmentVariable`.

Release notes: https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.1

## X single-post option

Elsa Workflows 3.7.1 is now available across Core, Studio, and Extensions.

This stable patch improves Azure Service Bus clustered hosting reliability, reduces entity-name pressure, fixes Quartz durable trigger scheduling, and aligns Studio/Extensions packages with 3.7.1.

https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.1

## X thread option

1/ Elsa Workflows 3.7.1 is now available across Core, Studio, and Extensions.

This stable patch focuses on production reliability for clustered hosting, Azure Service Bus deployments, Quartz scheduling, and package alignment.

https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.1

2/ Core adds stable application instance names via `ApplicationInstanceOptions.InstanceName` and `InstanceNameEnvironmentVariable`, helping Azure Service Bus-backed clustered hosts reuse subscriptions and queues across restarts.

3/ Extensions reduces Azure Service Bus entity-name pressure by shortening MassTransit endpoint suffixes, and fixes Quartz-backed scheduling by ensuring the durable workflow job exists before triggers are scheduled.

4/ Studio now consumes the published `Elsa.Api.Client` 3.7.1 package, while Extensions aligns with the Elsa Core and Studio 3.7.1 package set.

## Links

- Core: https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.1
- Studio: https://github.com/elsa-workflows/elsa-studio/releases/tag/3.7.1
- Extensions: https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.7.1
- NuGet package search: https://www.nuget.org/packages?q=elsa
