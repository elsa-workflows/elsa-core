# Reconcile workflow-as-activity registries with durable generations

Date: 2026-09-25

## Status

Accepted

## Context

Each node keeps workflow-as-activity descriptors in its local activity registry. The management mediator refreshes the registry on the node that processes a definition change, but mediator notifications and the registry lock are process-local. They do not tell another pod that its descriptors are stale. A node can also be offline during a change, or a process can stop after persisting a definition but before publishing a notification.

## Decision

Persist a monotonically increasing generation for each tenant and a separate `*` generation for tenant-agnostic definitions in the shared management database. After handling a workflow-definition mutation, advance the affected generation. Runtime nodes poll the current tenant and `*` generations every five seconds, using two indexed reads per active tenant per replica. A changed generation causes the node to invalidate its tenant-scoped workflow-definition query cache and replace the current tenant's plus agnostic descriptors from the authoritative store. Polling is configurable through the recurring-task schedule. A monotonic one-minute full-reconciliation timer repairs missed generation increments and lets an offline node catch up from the latest shared state.

Ambiguous bulk and deletion notifications carry no tenant IDs; their handlers advance both the ambient tenant and `*` generations because tenant-scoped store operations can also affect tenant-agnostic definitions. Reconciliation clears the visible provider set before adding the fetched set, including when it is empty, and fetches successfully before mutating the live registry.

## Consequences

Registry changes are eventually consistent across pods. With the default schedule, ordinary changes are observed within one poll interval plus query and reconciliation time; the periodic full reconciliation bounds recovery from missed signals to about one minute. The protocol requires every node to share both the generation database and workflow-definition source. The default in-memory generation store is node-local and does not enable cross-pod synchronization.

The definition write and generation increment are not one transaction for arbitrary external stores. A crash between them can delay peers until full reconciliation, assuming the definition source already exposes the mutation. Two indexed generation reads per active tenant per pod every five seconds are the polling cost. A combined-read optimization was not retained after the scheduled three-pod test failed to converge; diagnostics showed matching generation tuples on all pods, so the query was not established as the cause. These convergence bounds assume a healthy shared store, a running scheduler, and successful generation and definition queries; failures can extend the delay.
