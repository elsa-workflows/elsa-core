/**
 * Loads the `.bpmn` + `.activity.json` fixture pairs. See ../__fixtures__/README.md for what each
 * one covers and how the `.activity.json` half was captured.
 *
 * The pairs are read off disk rather than imported as modules so that the `.bpmn` half is available
 * as the exact text Studio receives on `CustomProperties["Bpmn:SourceXml"]`, and so that a test can
 * independently re-read the document's DI without going through the reader it is checking.
 */
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { BpmnActivity } from '../model';

const FIXTURES_DIRECTORY = join(dirname(fileURLToPath(import.meta.url)), '..', '__fixtures__');

export const FIXTURE_NAMES = [
    'camunda-order-process',
    'gateway-routing',
    'non-interrupting-error-event-subprocess',
    'publish-gate-process',
    'subprocess-boundary-events',
    'task-kinds-and-lanes',
    'transaction-compensation',
] as const;

export type FixtureName = (typeof FIXTURE_NAMES)[number];

export interface Fixture {
    readonly name: FixtureName;
    /** The root `Elsa.BpmnProcess` activity JSON, as elsa-core's import produced it. */
    readonly activity: BpmnActivity;
    /** The source document, as `CustomProperties["Bpmn:SourceXml"]` carries it. */
    readonly sourceXml: string;
}

export function loadFixture(name: FixtureName): Fixture {
    return {
        name,
        activity: JSON.parse(readFileSync(join(FIXTURES_DIRECTORY, `${name}.activity.json`), 'utf8')) as BpmnActivity,
        sourceXml: readFileSync(join(FIXTURES_DIRECTORY, `${name}.bpmn`), 'utf8'),
    };
}

/**
 * Pulls the ids a document's `BPMNShape` / `BPMNEdge` elements draw straight out of the XML text.
 *
 * Deliberately a regex over the raw source, not a call into `di-reader.ts`: a fixture test that
 * asked the reader what the document contains and then checked the answer against itself would pass
 * for a reader that silently found nothing.
 */
export function readDiagramReferences(sourceXml: string): { shapes: string[]; edges: string[] } {
    return {
        shapes: matchAll(sourceXml, /<bpmndi:BPMNShape\b[^>]*\bbpmnElement="([^"]+)"/g),
        edges: matchAll(sourceXml, /<bpmndi:BPMNEdge\b[^>]*\bbpmnElement="([^"]+)"/g),
    };
}

function matchAll(text: string, pattern: RegExp): string[] {
    return Array.from(text.matchAll(pattern), match => match[1]);
}
