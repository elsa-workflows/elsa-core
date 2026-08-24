# 27. Project User Tasks from committed workflow bookmarks

Date: 2026-08-17

## Status

Accepted

## Context

A User Task must both suspend workflow execution durably and support efficient task-inbox queries. Writing a bookmark and a separate task store as an uncoordinated dual write can leave an invisible suspended workflow or an orphan task. Making the task table the workflow source of truth would bypass Elsa's established bookmark execution semantics.

## Decision

The User Task activity creates a dedicated bookmark whose payload contains the materialized task definition. After the workflow commit succeeds, a projector idempotently creates the task record from the committed bookmark. Terminal task operations persist an idempotent transitional operation and asynchronously resume the bookmark with a dedicated stimulus. Bookmark removal finalizes the task.

A bounded, multi-node-safe reconciler scans committed User Task bookmarks and task operations to recreate missed projections, retry stale terminal delivery, and diagnose or finalize orphan records. Materialization key, bookmark ID, expected revision, and operation ID provide uniqueness and race control.

## Consequences

Workflow state remains authoritative for suspension and resumption, while the task store can be indexed independently for secure queues. Task availability may be briefly eventually consistent after workflow commit, and the module must operate a projector, operation outbox, and reconciler. Every provider must preserve the same idempotency and compare-and-swap guarantees.
