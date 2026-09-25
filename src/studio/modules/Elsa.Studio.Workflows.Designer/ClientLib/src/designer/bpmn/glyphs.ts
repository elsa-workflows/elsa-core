/**
 * The SVG path data for BPMN's markers: event-definition icons, task-type icons, gateway markers
 * and the activity markers that run along the bottom of a task or subprocess.
 *
 * Every path is authored centred on its own origin, in a nominal 16x16 box, so that a shape can
 * place it with nothing but X6's `refX`/`refY` -- no per-node arithmetic, no transform strings, and
 * the same glyph reusable at a corner, at the centre, or in the marker row.
 *
 * These live in the adapter rather than in `src/bpmn` on purpose: which shape a `timer` event
 * definition draws is a fact about *this* canvas. A React Flow adapter would render the same
 * definition as a React icon component, not as a path string, so there is nothing here for the two
 * to share. What *is* canvas-neutral -- the element's kind, its event definitions, whether a
 * boundary event interrupts -- comes from the view model and is never re-derived here.
 */

/**
 * How a glyph is painted.
 *
 * `'direction'` is BPMN's own rule for event definitions: the same definition is drawn hollow on an
 * event that catches it and solid on an event that throws it. `'outline'` and `'solid'` are for the
 * glyphs where that rule does not apply -- a timer only ever catches, a terminate only ever ends --
 * and for the markers that are not event definitions at all.
 */
export type GlyphFill = 'outline' | 'solid' | 'direction';

export interface Glyph {
    readonly d: string;
    readonly fill: GlyphFill;
}

const outline = (d: string): Glyph => ({ d, fill: 'outline' });
const solid = (d: string): Glyph => ({ d, fill: 'solid' });
const directional = (d: string): Glyph => ({ d, fill: 'direction' });

/**
 * Event-definition icons, keyed by the lowercase `type` the payload carries on
 * `BpmnElement.eventDefinitions` (`timer`, `message`, `error`, ... -- see `Bpmn.Model`).
 *
 * An unknown definition draws no icon rather than a wrong one; the event's ring still tells the
 * reader what kind of event it is.
 */
export const EVENT_DEFINITION_GLYPHS: Readonly<Record<string, Glyph>> = {
    message: directional('M -7 -5 L 7 -5 L 7 5 L -7 5 Z M -7 -5 L 0 1 L 7 -5'),
    timer: outline('M 0 -7 A 7 7 0 1 0 0.01 -7 Z M 0 -4.5 L 0 0 L 3 2'),
    error: directional('M -6 7 L -2 -4 L 2 1 L 6 -7 L 2 4 L -2 -1 Z'),
    escalation: directional('M 0 -7 L 6 7 L 0 1 L -6 7 Z'),
    cancel: directional('M -6.5 -5 L -5 -6.5 L 0 -1.5 L 5 -6.5 L 6.5 -5 L 1.5 0 L 6.5 5 L 5 6.5 L 0 1.5 L -5 6.5 L -6.5 5 L -1.5 0 Z'),
    compensation: directional('M -7 0 L -1 -4.5 L -1 4.5 Z M -1 0 L 5 -4.5 L 5 4.5 Z'),
    signal: directional('M 0 -6 L 7 6 L -7 6 Z'),
    terminate: solid('M -7 0 A 7 7 0 1 0 7 0 A 7 7 0 1 0 -7 0 Z'),
    conditional: outline('M -6 -7 L 6 -7 L 6 7 L -6 7 Z M -4 -4 L 4 -4 M -4 -1 L 4 -1 M -4 2 L 4 2 M -4 5 L 1 5'),
    link: directional('M -7 -3 L 1 -3 L 1 -6 L 7 0 L 1 6 L 1 3 L -7 3 Z'),
    multiple: directional('M 0 -7 L 7 -2 L 4 7 L -4 7 L -7 -2 Z'),
    parallelmultiple: outline('M -2 -7 L 2 -7 L 2 -2 L 7 -2 L 7 2 L 2 2 L 2 7 L -2 7 L -2 2 L -7 2 L -7 -2 L -2 -2 Z'),
};

/** The task-type icon drawn in a task's top-left corner. A plain `task` draws none. */
export const TASK_TYPE_GLYPHS: Readonly<Record<string, Glyph>> = {
    userTask: outline('M 0 -6.5 A 2.8 2.8 0 1 0 0.01 -6.5 Z M -5.5 7 C -5.5 1 5.5 1 5.5 7'),
    // An eight-tooth gear with a hub, generated rather than eyeballed so the teeth are even.
    serviceTask: outline('M -1.18 -5.27 L -1.19 -7.51 L 1.19 -7.51 L 1.18 -5.27 L 2.89 -4.56 L 4.47 -6.15 L 6.15 -4.47 L 4.56 -2.89 L 5.27 -1.18 L 7.51 -1.19 L 7.51 1.19 L 5.27 1.18 L 4.56 2.89 L 6.15 4.47 L 4.47 6.15 L 2.89 4.56 L 1.18 5.27 L 1.19 7.51 L -1.19 7.51 L -1.18 5.27 L -2.89 4.56 L -4.47 6.15 L -6.15 4.47 L -4.56 2.89 L -5.27 1.18 L -7.51 1.19 L -7.51 -1.19 L -5.27 -1.18 L -4.56 -2.89 L -6.15 -4.47 L -4.47 -6.15 L -2.89 -4.56 Z M 2.6 0 A 2.6 2.6 0 1 0 -2.6 0 A 2.6 2.6 0 1 0 2.6 0 Z'),
    scriptTask: outline('M -5 -7 L 5 -7 L 5 7 L -5 7 Z M -3 -4 L 3 -4 M -3 -1 L 3 -1 M -3 2 L 1 2'),
    manualTask: outline('M -6.5 2 L -6.5 -1 C -6.5 -3 -3.5 -3 -3.5 -1 L -3.5 -5 C -3.5 -7 -0.5 -7 -0.5 -5 L -0.5 -4 C -0.5 -6 2.5 -6 2.5 -4 L 2.5 -3 C 2.5 -5 5.5 -5 5.5 -3 L 5.5 2 C 5.5 5 3 7 0 7 L -3 7 C -5 7 -6.5 5 -6.5 2 Z'),
    businessRuleTask: outline('M -7 -5 L 7 -5 L 7 5 L -7 5 Z M -7 -2 L 7 -2 M -7 1.5 L 7 1.5 M -1 -2 L -1 5'),
    sendTask: solid('M -7 -5 L 7 -5 L 7 5 L -7 5 Z'),
    receiveTask: outline('M -7 -5 L 7 -5 L 7 5 L -7 5 Z M -7 -5 L 0 1 L 7 -5'),
};

/** The marker drawn in the middle of a gateway's diamond. */
export const GATEWAY_GLYPHS: Readonly<Record<string, Glyph>> = {
    exclusiveGateway: solid('M -6.5 -8 L 0 -3 L 6.5 -8 L 8 -6.5 L 3 0 L 8 6.5 L 6.5 8 L 0 3 L -6.5 8 L -8 6.5 L -3 0 L -8 -6.5 Z'),
    parallelGateway: solid('M -2.5 -10 L 2.5 -10 L 2.5 -2.5 L 10 -2.5 L 10 2.5 L 2.5 2.5 L 2.5 10 L -2.5 10 L -2.5 2.5 L -10 2.5 L -10 -2.5 L -2.5 -2.5 Z'),
    inclusiveGateway: outline('M 0 -9 A 9 9 0 1 0 0.01 -9 Z'),
    eventBasedGateway: outline('M 0 -9.5 A 9.5 9.5 0 1 0 0.01 -9.5 Z M 0 -7.5 A 7.5 7.5 0 1 0 0.01 -7.5 Z M 0 -5 L 4.8 -1.5 L 3 4 L -3 4 L -4.8 -1.5 Z'),
};

/** The markers that run along the bottom edge of a task, subprocess or call activity. */
export const ACTIVITY_MARKER_GLYPHS = {
    /** A subprocess the diagram draws collapsed. */
    collapsed: outline('M -6 -6 L 6 -6 L 6 6 L -6 6 Z M 0 -3.5 L 0 3.5 M -3.5 0 L 3.5 0'),
    /** A parallel multi-instance activity. */
    multiInstanceParallel: solid('M -5.5 -6 L -3 -6 L -3 6 L -5.5 6 Z M -1.25 -6 L 1.25 -6 L 1.25 6 L -1.25 6 Z M 3 -6 L 5.5 -6 L 5.5 6 L 3 6 Z'),
    /** A sequential multi-instance activity. */
    multiInstanceSequential: solid('M -6 -5.5 L 6 -5.5 L 6 -3 L -6 -3 Z M -6 -1.25 L 6 -1.25 L 6 1.25 L -6 1.25 Z M -6 3 L 6 3 L 6 5.5 L -6 5.5 Z'),
    /** A compensation handler. */
    compensation: solid('M -6.5 0 L -0.5 -4 L -0.5 4 Z M -0.5 0 L 5.5 -4 L 5.5 4 Z'),
} satisfies Readonly<Record<string, Glyph>>;

/**
 * The glyph for one event's definitions.
 *
 * BPMN allows several definitions on one event; the first is drawn, and `multiple` /
 * `parallelMultiple` is drawn when there is more than one and the document did not already say so.
 * Definition type matching is case-insensitive because the payload's `type` is the library's
 * spelling and this table's keys are ours.
 */
export function resolveEventGlyph(definitionTypes: readonly string[]): Glyph | null {
    if (definitionTypes.length === 0) return null;

    if (definitionTypes.length > 1) {
        return EVENT_DEFINITION_GLYPHS.multiple;
    }

    return EVENT_DEFINITION_GLYPHS[definitionTypes[0].toLowerCase()] ?? null;
}
