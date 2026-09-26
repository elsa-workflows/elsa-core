/**
 * Type-checks every captured `.activity.json` fixture against the shape `buildBpmnViewModel`
 * actually consumes, with no cast anywhere in the assignments below.
 *
 * `../__tests__/process-payload.test-d.ts` asks the narrower question -- does
 * `BpmnProcessDefinition` still describe the `process` payload -- for one fixture, and asks it
 * exhaustively (no excess keys at any depth). This file asks the wider one for all of them: the
 * *whole* root activity, including the nested `Elsa.BpmnProcess` activities that are the subprocess
 * bodies, has to satisfy `BpmnActivity`, whose `process` and `activities` properties are what make
 * that recursion type-checked. If a `Bpmn.Model` version bump changes the payload in a way the
 * generated types no longer cover, `npm test` stops compiling here.
 *
 * Runtime assertions about these same fixtures live in ./diagram-fixtures.test.ts.
 */
import type { BpmnActivity } from '../model';
import camundaOrderProcess from '../__fixtures__/camunda-order-process.activity.json';
import gatewayRouting from '../__fixtures__/gateway-routing.activity.json';
import nonInterruptingErrorEventSubprocess from '../__fixtures__/non-interrupting-error-event-subprocess.activity.json';
import publishGateProcess from '../__fixtures__/publish-gate-process.activity.json';
import subprocessBoundaryEvents from '../__fixtures__/subprocess-boundary-events.activity.json';
import taskKindsAndLanes from '../__fixtures__/task-kinds-and-lanes.activity.json';
import transactionCompensation from '../__fixtures__/transaction-compensation.activity.json';

const fixtures: BpmnActivity[] = [
    camundaOrderProcess,
    gatewayRouting,
    nonInterruptingErrorEventSubprocess,
    publishGateProcess,
    subprocessBoundaryEvents,
    taskKindsAndLanes,
    transactionCompensation,
];

void fixtures;
