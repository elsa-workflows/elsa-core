/**
 * The instance overlay, and the two ways it can quietly lie.
 *
 * A stats badge is the only part of the canvas that changes while a user watches it, and both of
 * its failure modes look like success: a fault that stops being drawn because something else about
 * the element also became true, and a cancellation drawn as a fault because the two share a colour.
 * Both are asserted here in both directions.
 */
import { describe, expect, it } from 'vitest';
import { flowTakenLineAttrs, statsBadgeAttrs } from '../cells';
import { BADGE_SURFACE_BY_TONE, EDGE } from '../palette';
import { isBpmnFlowTaken, resolveBpmnStatsBadge } from '../stats';

describe('resolveBpmnStatsBadge', () => {
    it('says nothing about an element the overlay says nothing about', () => {
        expect(resolveBpmnStatsBadge(null, null)).toBeNull();
        expect(resolveBpmnStatsBadge(undefined, undefined)).toBeNull();
    });

    it('distinguishes an element that has not run from an element nobody is watching', () => {
        // A definition carries no stats and gets no badges; an instance in which this element has
        // not been reached yet carries a zeroed entry and gets one. Collapsing the two would make a
        // running instance look like a definition.
        expect(resolveBpmnStatsBadge({}, null)).toEqual({ tone: 'idle', count: 0, title: 'Not started (0)' });
    });

    it('keeps a fault visible even when the element also completed', () => {
        const badge = resolveBpmnStatsBadge({ started: 3, completed: 2, faulted: true }, null);

        expect(badge?.tone).toBe('faulted');
        expect(badge?.count).toBe(3);
    });

    it('keeps a fault visible when it is the bound activity that faulted', () => {
        expect(resolveBpmnStatsBadge({ started: 1, completed: 1 }, { started: 1, completed: 1, faulted: true })?.tone)
            .toBe('faulted');
    });

    it('reports a cancellation as a cancellation, not as a failure', () => {
        const badge = resolveBpmnStatsBadge({ started: 1, canceled: true }, null);

        expect(badge?.tone).toBe('canceled');
        expect(badge?.title).toBe('Canceled (1)');
        expect(BADGE_SURFACE_BY_TONE.canceled).not.toBe(BADGE_SURFACE_BY_TONE.faulted);
    });

    it('still reports a fault on an element that was also cancelled', () => {
        // The other direction of the same question: cancelling on the way out of a fault must not
        // hide the fault that caused it.
        expect(resolveBpmnStatsBadge({ started: 1, canceled: true, faulted: true }, null)?.tone).toBe('faulted');
    });

    it('prefers blocked to running, and running to completed', () => {
        expect(resolveBpmnStatsBadge({ started: 2, completed: 1, active: 1, blocked: true }, null)?.tone).toBe('blocked');
        expect(resolveBpmnStatsBadge({ started: 2, completed: 1, active: 1 }, null)?.tone).toBe('active');
        expect(resolveBpmnStatsBadge({ started: 1, completed: 1, active: 0 }, null)?.tone).toBe('completed');
    });

    it('reads the flowchart\'s own counters when only an activity is being watched', () => {
        // `uncompleted` is the flowchart's name for what BPMN calls active, so a bound activity with
        // work outstanding lights up the same on both canvases.
        expect(resolveBpmnStatsBadge(null, { started: 2, completed: 1, uncompleted: 1 })?.tone).toBe('active');
        expect(resolveBpmnStatsBadge(null, { started: 1, completed: 1, uncompleted: 0 })?.tone).toBe('completed');
        expect(resolveBpmnStatsBadge(null, { blocked: true, started: 1 })?.tone).toBe('blocked');
    });
});

describe('statsBadgeAttrs', () => {
    it('clears the badge when the overlay no longer mentions the element', () => {
        // The dangerous direction: a fault that disappears from the map has to disappear from the
        // canvas. Attributes that merely stopped being written would leave the old badge on screen.
        const attrs = statsBadgeAttrs(null);

        expect(attrs.statsBadge.display).toBe('none');
        expect(attrs.statsLabel.display).toBe('none');
        expect(attrs.statsLabel.text).toBe('');
    });

    it('draws the badge in its own tone, with its count', () => {
        const attrs = statsBadgeAttrs(resolveBpmnStatsBadge({ started: 4, faulted: true }, null));

        expect(attrs.statsBadge.display).toBe('block');
        expect(attrs.statsBadge.fill).toBe(BADGE_SURFACE_BY_TONE.faulted);
        expect(attrs.statsLabel.text).toBe('4');
    });

    it('writes the same attribute keys whether the badge is drawn or cleared', () => {
        // Both paths must touch the same selectors, or re-applying one after the other would leave
        // half the previous badge behind.
        expect(Object.keys(statsBadgeAttrs(null)).sort())
            .toEqual(Object.keys(statsBadgeAttrs(resolveBpmnStatsBadge({ completed: 1 }, null))).sort());
    });

    // The badge is the only part of the canvas that tells faulted, cancelled and blocked apart by
    // colour alone; the title is what lets a tooltip, or a screen reader, say the same thing in words.
    it('gives a faulted badge its title as a tooltip and an aria-label', () => {
        const attrs = statsBadgeAttrs(resolveBpmnStatsBadge({ started: 1, faulted: true }, null));

        expect(attrs.statsBadgeTitle.text).toBe('Faulted (1)');
        expect(attrs.statsBadgeGroup['aria-label']).toBe('Faulted (1)');
    });

    it('gives a cancelled badge its title as a tooltip and an aria-label', () => {
        const attrs = statsBadgeAttrs(resolveBpmnStatsBadge({ started: 1, canceled: true }, null));

        expect(attrs.statsBadgeTitle.text).toBe('Canceled (1)');
        expect(attrs.statsBadgeGroup['aria-label']).toBe('Canceled (1)');
    });

    it('gives a blocked badge its title as a tooltip and an aria-label', () => {
        const attrs = statsBadgeAttrs(resolveBpmnStatsBadge({ started: 1, blocked: true }, null));

        expect(attrs.statsBadgeTitle.text).toBe('Blocked (1)');
        expect(attrs.statsBadgeGroup['aria-label']).toBe('Blocked (1)');
    });
});

describe('isBpmnFlowTaken', () => {
    it('says no about a flow the overlay says nothing about', () => {
        expect(isBpmnFlowTaken(null)).toBe(false);
        expect(isBpmnFlowTaken(undefined)).toBe(false);
        expect(isBpmnFlowTaken({})).toBe(false);
    });

    it('says yes once the flow has been taken, by either counter the projector might set', () => {
        expect(isBpmnFlowTaken({ completed: 1 })).toBe(true);
        expect(isBpmnFlowTaken({ started: 1 })).toBe(true);
    });
});

describe('flowTakenLineAttrs', () => {
    it('paints an untaken flow in the plain edge colour, at the plain width', () => {
        expect(flowTakenLineAttrs(null)).toEqual({ stroke: EDGE, strokeWidth: 1.5 });
        expect(flowTakenLineAttrs({})).toEqual({ stroke: EDGE, strokeWidth: 1.5 });
    });

    it('paints a taken flow in the same tone a completed node badge uses, thicker', () => {
        const attrs = flowTakenLineAttrs({ completed: 1 });

        expect(attrs.stroke).toBe(BADGE_SURFACE_BY_TONE.completed);
        expect(attrs.strokeWidth).toBeGreaterThan(1.5);
    });

    it('returns both properties whichever way it decides, so a flow that stops being taken falls back instead of keeping its old colour', () => {
        // updateBpmnElementStats merges this into the edge's existing `line` attrs rather than
        // replacing them; a partial object here would leave a cleared flow looking taken forever.
        expect(Object.keys(flowTakenLineAttrs(null)).sort()).toEqual(Object.keys(flowTakenLineAttrs({ completed: 1 })).sort());
    });
});
