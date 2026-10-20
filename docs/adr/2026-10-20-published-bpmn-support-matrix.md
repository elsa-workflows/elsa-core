# Publish the supported BPMN subset as a tested matrix

Date: 2026-10-20

## Status

Proposed

## Context

#7909 delivered BPMN v1 as "Camunda's executable subset, within reason". Which elements that covers is spread over the
library's reader findings, the binder, the host and the closing comment of #7909. Several elements reported as
supported have no runtime test: inclusive and event-based gateways, message and signal boundary events, call activities,
and, until it was fixed, a process started by a trigger. That last gap hid a defect that made every message, signal or
timer start fault.

Users need to know what they can model and expect to run, and contributors need to know what they must not break.

## Decision

Elsa publishes a BPMN support matrix and keeps it true.

- The matrix lists each BPMN element and event definition with one of four states: Supported, Partial (with what is
  missing), Degraded or Dropped. It lives in the BPMN documentation.
- Every Supported or Partial row names the runtime test that proves it, in `Elsa.Bpmn.IntegrationTests`. A row without a
  test is not Supported.
- Changing a row's state requires changing its test in the same pull request.
- Elsa's BPMN interoperability claims are limited to the matrix. Import and export round-trip is checked in CI against
  the BPMN Model Interchange Working Group (MIWG) reference models for the elements the matrix supports.

## Consequences

- Support claims are checkable, and regressions in a Supported element fail CI.
- Adding BPMN support costs a test and a documentation row, which is intended.
- The initial matrix will show more Partial rows than the v1 summary implied.
