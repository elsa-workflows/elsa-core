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
