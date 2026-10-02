/**
 * The instance-state badge: how the two stats maps the view model resolves onto an element turn
 * into one badge on the canvas.
 *
 * The visual language is the flowchart designer's -- `Components/ActivityWrappers/V1` and `V2`'s `ActivityWrapper.razor`
 * puts a MudBadge on the activity, coloured error / warning / info / success and captioned with a
 * count -- so that an instance diagram reads the same whichever designer drew it. What is new here
 * is `canceled`: `BpmnElementStats` reports a cancelled token, the flowchart's `ActivityStats` has
 * no such concept, and a cancelled element must not be shown as a faulted one. That is the case
 * this module is most careful about, and the one `__tests__/stats.test.ts` checks in both
 * directions.
 */
import type { BpmnActivityStats, BpmnElementStats } from '../../bpmn';

/**
 * What the badge says, in the order it is decided.
 *
 * The order is a precedence, not a preference: a fault outranks everything because losing it is the
 * one mistake that makes a broken instance look healthy; a cancellation outranks the running and
 * completed states because a cancelled element did not complete; `idle` is the state of an element
 * the overlay knows about but that has not run.
 */
export type BpmnStatsTone = 'faulted' | 'canceled' | 'blocked' | 'active' | 'completed' | 'idle';

export interface BpmnStatsBadge {
    readonly tone: BpmnStatsTone;
    /** The number in the badge. Null when no counter applies, which draws an empty dot. */
    readonly count: number | null;
    /** A human-readable summary, used as the node's tooltip. */
    readonly title: string;
}

const TONE_TITLES: Readonly<Record<BpmnStatsTone, string>> = {
    faulted: 'Faulted',
    canceled: 'Canceled',
    blocked: 'Blocked',
    active: 'Running',
    completed: 'Completed',
    idle: 'Not started',
};

/**
 * Resolves one element's badge, or null when the overlay says nothing about it at all.
 *
 * Null and "everything is zero" are different answers on purpose: a workflow definition that was
 * never run carries no stats and gets no badges, while an instance in which one element has not yet
 * been reached carries a zeroed entry and gets an `idle` badge. Collapsing the two would make a
 * diagram of a running instance look like a diagram of a definition.
 */
export function resolveBpmnStatsBadge(
    elementStats: BpmnElementStats | null | undefined,
    activityStats: BpmnActivityStats | null | undefined): BpmnStatsBadge | null {
    if (elementStats == null && activityStats == null) return null;

    const started = firstNumber(elementStats?.started, activityStats?.started);
    const completed = firstNumber(elementStats?.completed, activityStats?.completed);
    const active = firstNumber(elementStats?.active, activityStats?.uncompleted);

    if (elementStats?.faulted === true || activityStats?.faulted === true) return badge('faulted', started ?? completed);
    if (elementStats?.canceled === true) return badge('canceled', started ?? completed);
    if (elementStats?.blocked === true || activityStats?.blocked === true) return badge('blocked', started ?? completed);
    if ((active ?? 0) > 0) return badge('active', active);
    if ((completed ?? 0) > 0) return badge('completed', completed);

    return badge('idle', completed ?? started ?? 0);
}

function badge(tone: BpmnStatsTone, count: number | null): BpmnStatsBadge {
    return { tone, count, title: count == null ? TONE_TITLES[tone] : `${TONE_TITLES[tone]} (${count})` };
}

function firstNumber(...values: readonly (number | null | undefined)[]): number | null {
    for (const value of values) {
        if (typeof value === 'number' && Number.isFinite(value)) return value;
    }

    return null;
}

/**
 * Whether a sequence flow's own stats entry says it has been taken at least once.
 *
 * A flow has no state beyond that: `BpmnElementStatsProjector` (the C# side, the one place this
 * mapping lives) marks a flow's entry the moment a token travels along it, by its own flow id, so
 * either counter being positive is enough.
 */
export function isBpmnFlowTaken(stats: BpmnElementStats | null | undefined): boolean {
    return (stats?.completed ?? 0) > 0 || (stats?.started ?? 0) > 0;
}
