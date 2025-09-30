

- **Bulk provisioning & isolation** — tests should support bulk import of workflow definitions for large-suite runs and ensure clean, isolated state per test.
- **End‑to‑end tests**: Deploy Elsa Server (or host app) in Docker/K8s with a real DB, then run workflows via REST and assert via durable traces (journal/DB/events). Keep E2E suite small and targeted.