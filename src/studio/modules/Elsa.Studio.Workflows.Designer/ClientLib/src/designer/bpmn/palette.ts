/**
 * The colours the BPMN shapes paint with, as CSS custom properties.
 *
 * Every value is a `var(...)` reference resolved by `css/designer.bpmn.css`, which is scoped to the
 * class `createBpmnGraph` puts on the container. SVG presentation attributes accept custom
 * properties, so a theme change repaints the canvas without the adapter being told -- the same
 * mechanism `internal/state-machine-shapes.ts` already relies on.
 */
import type { BpmnStatsTone } from './stats';

export const CANVAS = 'var(--elsa-bpmn-canvas)';
export const GRID = 'var(--elsa-bpmn-grid)';
export const STROKE = 'var(--elsa-bpmn-stroke)';
export const SURFACE = 'var(--elsa-bpmn-surface)';
export const TEXT = 'var(--elsa-bpmn-text)';
export const MUTED = 'var(--elsa-bpmn-muted)';
export const EDGE = 'var(--elsa-bpmn-edge)';
export const CONTAINER_SURFACE = 'var(--elsa-bpmn-container-surface)';
export const CONTAINER_STROKE = 'var(--elsa-bpmn-container-stroke)';
export const HEADER_SURFACE = 'var(--elsa-bpmn-header-surface)';
/** The colour an element whose work is not bound, or not resolvable, is called out in. */
export const UNBOUND = 'var(--elsa-bpmn-unbound)';

export const BADGE_SURFACE_BY_TONE: Readonly<Record<BpmnStatsTone, string>> = {
    faulted: 'var(--elsa-bpmn-badge-faulted)',
    canceled: 'var(--elsa-bpmn-badge-canceled)',
    blocked: 'var(--elsa-bpmn-badge-blocked)',
    active: 'var(--elsa-bpmn-badge-active)',
    completed: 'var(--elsa-bpmn-badge-completed)',
    idle: 'var(--elsa-bpmn-badge-idle)',
};

export const BADGE_TEXT_BY_TONE: Readonly<Record<BpmnStatsTone, string>> = {
    faulted: 'var(--elsa-bpmn-badge-faulted-text)',
    canceled: 'var(--elsa-bpmn-badge-canceled-text)',
    blocked: 'var(--elsa-bpmn-badge-blocked-text)',
    active: 'var(--elsa-bpmn-badge-active-text)',
    completed: 'var(--elsa-bpmn-badge-completed-text)',
    idle: 'var(--elsa-bpmn-badge-idle-text)',
};
