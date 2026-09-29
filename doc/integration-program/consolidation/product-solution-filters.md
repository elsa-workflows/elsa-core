# Product-focused solution filters

`Elsa.sln` remains the full consolidated build and CI solution. The root [Extensions filter](../../../Elsa.Extensions.slnf) selects its 97 currently included source, test, and sample projects, and the [Studio filter](../../../Elsa.Studio.slnf) selects its 72 currently included projects. Both refer to the same root solution; they do not restore the old standalone solutions or create a shared package release unit.

Open or build a product slice from the consolidated checkout:

```sh
dotnet build Elsa.Extensions.slnf
dotnet build Elsa.Studio.slnf
```

Build the Studio filter without a forced `-f` because its included `BlazorApp1` sample targets `net8.0` while other projects multi-target. A framework-specific build can instead target an individual project that supports that framework.

The [WorkflowContexts debug filter](../../../Elsa.WorkflowContexts.Debug.slnf) remains the smaller paired backend/Blazor example. Use the root `Elsa.sln` for complete solution validation. The filter membership test compares both product filters to the root solution, catches duplicate/missing paths, and requires every selected project to exist:

```sh
python3 -m unittest scripts/integration-program/test_product_solution_filters.py
```

These filters select projects for local development; package release units and publisher ownership are defined separately by the integration program's release manifest and approval gates.
