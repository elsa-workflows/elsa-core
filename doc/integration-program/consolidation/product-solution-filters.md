# Solution grouping and focused filters

`Elsa.sln` remains the full consolidated build and CI solution. Its projects are grouped into solution folders by what
they are, not where they came from:

- **Foundation:** the workflow engine and what a minimal server needs. This includes the engine, management, runtime
  with its distributed implementation, the API, JavaScript and Liquid expressions, identity, tenants, scheduling,
  resilience, caching, HTTP, EF Core persistence with its providers, and Alterations, which EF Core persistence depends on.
- **Extensions/\<Domain\>:** the optional modules, whether they came from Core or were imported from Extensions. There are
  seven domains: Scripting, Persistence, Runtime, AI, Security, Operations and Integrations.
- **Studio**, **Apps** and **Samples**.

Each group has a `Tests` subfolder. A test sits with the project its name covers, otherwise with the single group it
references. `Elsa.Testing.*` harnesses sit with the Foundation tests.

The grouping is curated in [`scripts/solution/solution-groups.json`](../../../scripts/solution/solution-groups.json).
[`solution_groups.py`](../../../scripts/solution/solution_groups.py) generates the folders and these filters from it:

| Filter | Contents |
| --- | --- |
| `Elsa.Foundation.slnf` | Foundation and its tests |
| `Elsa.<Domain>.slnf` | Foundation plus one domain, one filter per domain |
| `Elsa.Studio.slnf` | Studio, Foundation and `Elsa.Api.Client` |
| `Elsa.Extensions.slnf` | Every optional domain, without Studio |

Each filter also includes the project-reference closure of what it selects, so it loads and builds on its own. For
example, the Foundation tests' harnesses pull in the C# expression and blob-storage workflow-provider projects. The
generator also enforces that Foundation source projects reference only Foundation projects.

```sh
dotnet build Elsa.Foundation.slnf
python3 scripts/solution/solution_groups.py          # regenerate after adding or moving a project
python3 scripts/solution/solution_groups.py --check  # what CI runs
```

A new project fails the check until its name or path is added to the manifest. The
[WorkflowContexts debug filter](../../../Elsa.WorkflowContexts.Debug.slnf) is hand-maintained and unaffected. These
filters are for local development; package release units and publisher ownership are defined separately.
