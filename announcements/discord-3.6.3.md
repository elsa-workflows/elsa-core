:rocket: **Elsa Workflows 3.6.3 is here!**

We've published the stable **Elsa 3.6.3** patch release across **Elsa Core**, **Elsa Studio**, and **Elsa Extensions**.

:point_right: Core: <https://github.com/elsa-workflows/elsa-core/releases/tag/3.6.3>
:point_right: Studio: <https://github.com/elsa-workflows/elsa-studio/releases/tag/3.6.3>
:point_right: Extensions: <https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.6.3>

Packages are available on NuGet.

### :sparkles: Highlights

:tools: **Azure Service Bus startup stability**
Core now supports stable application instance names for clustered hosts, plus deterministic shortening for long names.

:clock3: **Blank Cron expressions behave as disabled again**
Blank Cron values no longer block publishing or throw during workflow execution. Invalid nonblank Cron expressions still fail validation.

:memo: **Better workflow publish feedback**
Publish APIs now surface validation warnings and errors instead of reporting silent success.

:gear: **Quartz and MassTransit extension fixes**
Extensions now ensure durable Quartz jobs before scheduling triggers and use shorter MassTransit endpoint names.

:jigsaw: **Studio package alignment**
Studio now consumes `Elsa.Api.Client` 3.6.3.

### :tools: Upgrade notes

No intentional breaking changes. If you run clustered Elsa hosts on Azure Service Bus, review the stable application instance name option.

### :raised_hands: Feedback welcome

Please report upgrade issues or regressions so we can keep the 3.6 line solid.
