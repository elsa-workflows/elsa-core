# Persistence vNext Workflow Runtime Evaluation

## Decision

Persistence vNext is viable for Elsa management, catalog, metadata, admin-defined entity, and small module stores. It should not replace workflow runtime hot-path stores yet.

The current go/no-go decision is:

- Go: workflow definitions, metadata stores, secrets, labels, tenants, admin-defined entities, and other document-friendly module data.
- Conditional go: workflow instance management/query surfaces if provider benchmarks prove the declared indexes are fast enough, preferably with physicalization policies for common filters.
- No-go for now: bookmarks, triggers, bookmark queues, dispatch/outbox queues, activity execution records, and workflow execution logs.

## Why Runtime Stores Are Different

Workflow runtime stores are not only persistence concerns. They encode runtime semantics:

- bookmark and trigger lookup latency
- distributed locking
- queue visibility, retry, and dead-letter behavior
- append-heavy log/event volume
- cleanup and retention behavior
- concurrency under multiple workers

A portable document/index layer can represent these records, but representation alone is not enough. The provider must also prove that runtime semantics are correct and fast under load.

## Recommended Runtime Strategy

Keep specialized runtime providers until three conditions are met:

1. Representative benchmarks exist for SQLite, SQL Server, PostgreSQL, and MongoDB.
2. Provider contract tests validate concurrency, locking, retry, and cleanup behavior.
3. Hot-path storage units have explicit physicalization policies with rollback guidance.

Use Persistence vNext first for stores where the risk profile is lower:

- management/catalog data
- module configuration/state
- runtime-defined entities
- operational metadata
- queryable admin data

## Physicalization Requirement

If workflow instances or other high-volume records move to vNext, they should use portable documents by default in development, but production providers should be able to opt into:

- relational dedicated tables
- relational generated or typed index columns
- MongoDB dedicated collections
- native provider indexes
- retention/cleanup commands

This keeps module intent provider-neutral while letting providers own the physical performance shape.

## Required Evidence Before Migrating Hot Paths

Benchmarks should cover:

- workflow instance queries by status, sub-status, definition id, version, correlation id, and updated timestamp
- bookmark lookup by activity type, hash, workflow instance id, and tenant id
- trigger lookup by hash, workflow definition id, and tenant id
- queue dequeue/lease/retry/dead-letter behavior
- log append and retention cleanup throughput

Contract tests should cover:

- optimistic concurrency failures
- distributed lock behavior
- duplicate startup materialization
- worker retry behavior
- transactional index updates
- recovery after partial materialization failure

## Migration Strategy

1. Keep current EF Core runtime persistence intact.
2. Add vNext-backed stores only for low-risk module and management data.
3. Add benchmark fixtures for workflow runtime query shapes.
4. Add provider contract tests for locking, queues, and append-heavy logs.
5. Introduce physicalization policies for workflow instance and bookmark indexes.
6. Pilot one non-critical runtime-adjacent store.
7. Only migrate runtime hot paths after benchmarks and contract tests pass across the supported providers.

## POC Status

The POC now includes a small evaluator that codifies this decision. It classifies metadata stores as vNext candidates, hot distributed lookups as benchmark-gated, and queue/log workloads as specialized-provider territory for now.
