
## Goals / Non‑functional requirements

1. **Deterministic tests** — tests should not be flaky and should produce the same results independent of circumstance.
2. **Fast feedback** — unit and integration tests should run quickly to support local development workflows and CI.
3. **Minimal reliance on real delays** — avoid `Task.Delay`, `Thread.Sleep` or real clocks except where unavoidable; prefer event-driven assertions.
4. **Environment portability** — tests should run in local dev, Docker or any other container CI environment with minimal changes.
5. **Version alignment** — workflow definition versions and test artifacts must be explicitly linked so tests refer to a specific workflow definition version.
6. **Failure simulation** — deterministic ways to simulate activity or host failures and assert correct recovery/compensation.

---


- **Bulk provisioning & isolation** — tests should support bulk import of workflow definitions for large-suite runs and ensure clean, isolated state per test.
- **End‑to‑end tests**: Deploy Elsa Server (or host app) in Docker/K8s with a real DB, then run workflows via REST and assert via durable traces (journal/DB/events). Keep E2E suite small and targeted.

###  Importing and publishing workflow definitions for testing workflows

Two main patterns:

**A. Code‑first registration (recommended for component tests)**
- Register workflows as code inside test setup. This is fast and avoids serialization roundtrips.
- Good for tests that validate runtime behavior without involving persistence or designer serialization.

**B. Serialized definitions (recommended for integration & E2E tests)**
- Store JSON workflow definition artifacts in the `tests/definitions/` folder in the repo, commit them with semantic versions, and let tests import them into the engine via the same publish/import APIs used in production.
- Tests are responsible for *publishing* the definitions into the test host if the scenario requires persistence (e.g. testing versioned definitions or import behavior).

**Which to use?**
- Component tests: code-first definitions or workflows created via designer and exported to JSON.
- Integration/E2E tests: use serialized artifacts to validate persistence, designer output and versioning behavior.

###  Bulk import

- Implement a **test importer utility** that accepts a directory of workflow definition artifacts (JSON/YAML) and publishes them via the engine's public API or directly seeds the persistence store. The utility should:
    - Validate schema and version.
    - Report conflicts or duplicate IDs.
    - Run in parallel but enforce deterministic ordering when versions matter.
- For very large import workloads, support an optimized DB seed path used only in tests (direct DB insert) to avoid the overhead of the full publish pipeline. Mark this as *test-only*.


###  Working with Docker, env, K8s cluster deployments

- **Test strategy split**:
    - Local developer tests: use in‑process hosts and in‑memory/ephemeral DBs (SQLite in-memory or Testcontainers-based DB). Fast and deterministic.
    - CI Docker Compose: spin up a lightweight containerized environment with the host app, a real DB (Postgres, SQL Server or Mongo) and optional message broker; use Testcontainers (or Docker Compose) to orchestrate in CI.
    - K8s E2E: run a small suite that deploys a test namespace with Helm or apply manifests. Use ephemeral resources and ensure cleanup. Keep these tests in a separate CI stage.

- **Use Testcontainers** (or equivalent) to provision ephemeral DBs/brokers in CI; this keeps environments close to production while still being isolated and reproducible.

- **Configuration**: Keep environment variables and k8s manifests in `tests/ci/` and parametrize connection strings so tests can switch between in‑process and containerized runs.

### CI recommendations

- **Local unit tests**: run in `dotnet test` step (fast). Use in‑memory stores and determinism.
- **Integration tests**: run with Testcontainers (or similar) to provide real DB/broker. Run these in a separate CI job because they are slower.
- **K8s smoke tests**: optional separate stage. Deploy to ephemeral namespace via Helm and run a small suite of smoke E2E tests; tear down after.
- **Parallelization**: run multiple test matrices (DB providers) but avoid running heavy E2E jobs in parallel unless you have isolated resources.
- **Flaky test detection**: enable a flaky test retry policy for known non‑deterministic tests, but treat retries as signals to fix the underlying determinism problems.
