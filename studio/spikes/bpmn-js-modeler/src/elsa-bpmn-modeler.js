/**
 * SPIKE (#8901) -- the framework-neutral BPMN modeler module both Studios would host.
 *
 * Plain ES module, no Blazor or React code. A host creates one modeler per mounted canvas, talks to it through the
 * handle returned by `createModeler`, and calls `dispose()` when it unmounts the canvas. Elements are identified by
 * their BPMN element id everywhere (selection, change events, overlays), the same key `BpmnElementStats` uses.
 *
 * Throwaway quality: enough to measure what the ADR asks, not production code.
 */
import Modeler from 'bpmn-js/lib/Modeler';
import { BpmnPropertiesPanelModule, BpmnPropertiesProviderModule } from 'bpmn-js-properties-panel';
import ElsaPropertiesProviderModule from './elsa-properties-provider.js';
import elsaModdleDescriptor from './elsa-moddle.json';

/**
 * @typedef {{ message: string, elementId?: string }} ImportWarning
 * @typedef {{ imported: boolean, error?: string, warnings: ImportWarning[] }} LoadResult
 * @typedef {{ elementIds: string[], isDirty: boolean, canUndo: boolean, canRedo: boolean }} ChangeEvent
 * @typedef {{ elementIds: string[], primary: { id: string, type: string, name?: string } | null }} SelectionEvent
 * @typedef {{ started?: number, completed?: number, active?: number, blocked?: boolean, faulted?: boolean, canceled?: boolean }} ElementStats
 * @typedef {{ applied: string[], unknownElementIds: string[] }} OverlayResult
 */

const OVERLAY_TYPE = 'elsa-element-state';
const MARKERS = ['elsa-faulted', 'elsa-active', 'elsa-blocked', 'elsa-completed', 'elsa-canceled'];

/** The renderer colours resolve through CSS custom properties, so a theme switch needs no re-render. */
const THEMED_RENDERER = {
    defaultFillColor: 'var(--elsa-bpmn-shape-fill)',
    defaultStrokeColor: 'var(--elsa-bpmn-shape-stroke)',
    defaultLabelColor: 'var(--elsa-bpmn-label)'
};

/**
 * Creates a modeler in `container`, optionally with a properties panel in `propertiesContainer`.
 *
 * `preserveDiOrder` switches off bpmn-js's BpmnDiOrdering, which on every save rewrites each plane's `planeElement`
 * list from what is drawn on the canvas (re-ordering it, and dropping DI for anything that was not drawn).
 *
 * @param {HTMLElement} container the canvas host. The modeler owns its content until `dispose()`.
 * @param {{ propertiesContainer?: HTMLElement | null, themedRenderer?: boolean, preserveDiOrder?: boolean }} [options]
 */
export function createModeler(container, options = {}) {
    if (!(container instanceof HTMLElement)) throw new Error('createModeler needs a container element.');

    const withPanel = options.propertiesContainer instanceof HTMLElement;
    let modeler = new Modeler({
        container,
        moddleExtensions: { elsa: elsaModdleDescriptor },
        bpmnRenderer: options.themedRenderer === false ? undefined : THEMED_RENDERER,
        propertiesPanel: withPanel ? { parent: options.propertiesContainer } : undefined,
        additionalModules: [
            ...(withPanel ? [BpmnPropertiesPanelModule, BpmnPropertiesProviderModule, ElsaPropertiesProviderModule] : []),
            ...(options.preserveDiOrder ? [{ bpmnDiOrdering: ['value', {}] }] : [])
        ]
    });

    container.classList.add('elsa-bpmn-modeler');

    const eventBus = modeler.get('eventBus');
    const listeners = { change: new Set(), selection: new Set() };
    const busSubscriptions = [];
    const appliedMarkers = new Map();
    let pendingChangedIds = new Set();
    let savedStackIndex = -1;
    let loading = false;
    let disposed = false;

    function subscribe(event, priority, handler) {
        eventBus.on(event, priority, handler);
        busSubscriptions.push(() => eventBus.off(event, handler));
    }

    function emit(kind, payload) {
        for (const listener of [...listeners[kind]]) {
            try {
                listener(payload);
            } catch (error) {
                console.error(`elsa-bpmn-modeler: a ${kind} listener threw`, error);
            }
        }
    }

    function stackIndex() {
        return modeler.get('commandStack')._stackIdx;
    }

    subscribe('elements.changed', 1000, event => {
        for (const element of event.elements) {
            const id = element.businessObject?.id ?? element.id;
            if (id && !element.labelTarget) pendingChangedIds.add(id);
        }
    });

    subscribe('commandStack.changed', 1000, () => {
        if (loading) return;
        const commandStack = modeler.get('commandStack');
        const elementIds = [...pendingChangedIds];
        pendingChangedIds = new Set();
        emit('change', {
            elementIds,
            isDirty: stackIndex() !== savedStackIndex,
            canUndo: commandStack.canUndo(),
            canRedo: commandStack.canRedo()
        });
    });

    subscribe('selection.changed', 1000, event => {
        // Import clears the selection (several times); the host learns about a new document from load(), not from this.
        if (loading) return;
        const selected = event.newSelection.filter(element => !element.labelTarget);
        const primary = selected[0];
        emit('selection', {
            elementIds: selected.map(element => element.businessObject?.id ?? element.id),
            primary: primary
                ? { id: primary.businessObject?.id ?? primary.id, type: primary.type, name: primary.businessObject?.name }
                : null
        });
    });

    function ensureLive() {
        if (disposed) throw new Error('This modeler has been disposed.');
    }

    // Methods never use `this`: a JS-interop host may call them detached from the handle.
    function clearOverlays() {
        ensureLive();
        modeler.get('overlays').remove({ type: OVERLAY_TYPE });
        const canvas = modeler.get('canvas');
        for (const [shapeId, markers] of appliedMarkers) {
            const element = modeler.get('elementRegistry').get(shapeId);
            if (element) for (const marker of markers) canvas.removeMarker(element, marker);
        }
        appliedMarkers.clear();
    }

    function findElement(elementId) {
        const registry = modeler.get('elementRegistry');
        return registry.get(elementId) ?? registry.find(element => element.businessObject?.id === elementId && !element.labelTarget);
    }

    return {
        /**
         * Imports a BPMN document, replacing whatever the canvas held.
         * @param {string} xml
         * @returns {Promise<LoadResult>}
         */
        async load(xml) {
            ensureLive();
            loading = true;
            try {
                const { warnings } = await modeler.importXML(xml);
                savedStackIndex = stackIndex();
                pendingChangedIds = new Set();
                appliedMarkers.clear();
                modeler.get('canvas').zoom('fit-viewport', 'auto');
                return { imported: true, warnings: warnings.map(toWarning) };
            } catch (error) {
                return { imported: false, error: String(error?.message ?? error), warnings: (error?.warnings ?? []).map(toWarning) };
            } finally {
                loading = false;
            }
        },

        /**
         * Serialises the current document. Marks it clean.
         * @returns {Promise<string>}
         */
        async save() {
            ensureLive();
            const { xml } = await modeler.saveXML({ format: true });
            savedStackIndex = stackIndex();
            return xml;
        },

        /**
         * Selects the element with the given BPMN id, or clears the selection for `null`.
         * @param {string | null} elementId
         * @returns {boolean} whether the element exists
         */
        select(elementId) {
            ensureLive();
            const selection = modeler.get('selection');
            if (elementId == null) {
                selection.select(null);
                return true;
            }
            const element = findElement(elementId);
            if (!element) return false;
            selection.select(element);
            modeler.get('canvas').scrollToElement(element);
            return true;
        },

        /**
         * Subscribes to `change` or `selection`. Returns the unsubscribe function.
         * @param {'change' | 'selection'} kind
         * @param {(payload: ChangeEvent | SelectionEvent) => void} listener
         */
        on(kind, listener) {
            ensureLive();
            if (!listeners[kind]) throw new Error(`Unknown event '${kind}'.`);
            listeners[kind].add(listener);
            return () => listeners[kind]?.delete(listener);
        },

        /**
         * Replaces all instance-state overlays and markers with the given stats, keyed by BPMN element id.
         * @param {Record<string, ElementStats>} statsById
         * @returns {OverlayResult}
         */
        applyOverlays(statsById) {
            ensureLive();
            clearOverlays();
            const overlays = modeler.get('overlays');
            const canvas = modeler.get('canvas');
            const applied = [];
            const unknownElementIds = [];
            for (const [elementId, stats] of Object.entries(statsById ?? {})) {
                const element = findElement(elementId);
                if (!element) {
                    unknownElementIds.push(elementId);
                    continue;
                }
                const markers = markersFor(stats);
                for (const marker of markers) canvas.addMarker(element, marker);
                appliedMarkers.set(element.id, markers);
                overlays.add(element, OVERLAY_TYPE, { position: { top: -10, right: 10 }, html: badgeHtml(stats) });
                applied.push(elementId);
            }
            return { applied, unknownElementIds };
        },

        /** Removes every overlay and marker `applyOverlays` added. */
        clearOverlays,

        /** Releases the bpmn-js instance, its DOM and every handler. Idempotent. */
        dispose() {
            if (disposed) return;
            disposed = true;
            for (const unsubscribe of busSubscriptions.splice(0)) unsubscribe();
            listeners.change.clear();
            listeners.selection.clear();
            appliedMarkers.clear();
            modeler.destroy();
            container.classList.remove('elsa-bpmn-modeler');
            if (withPanel) options.propertiesContainer.replaceChildren();
            modeler = null;
        },

        /** Test hook only: the underlying bpmn-js instance. Not part of the proposed API. */
        get _bpmnjs() {
            return modeler;
        }
    };
}

function toWarning(warning) {
    const elementId = warning?.element?.id ?? warning?.context?.element?.id;
    return elementId ? { message: String(warning.message), elementId } : { message: String(warning?.message ?? warning) };
}

function markersFor(stats) {
    if (stats.faulted) return ['elsa-faulted'];
    if (stats.blocked) return ['elsa-blocked'];
    if ((stats.active ?? 0) > 0) return ['elsa-active'];
    if (stats.canceled) return ['elsa-canceled'];
    if ((stats.completed ?? 0) > 0) return ['elsa-completed'];
    return [];
}

function badgeHtml(stats) {
    const badge = document.createElement('div');
    badge.className = `elsa-bpmn-badge ${markersFor(stats)[0] ?? 'elsa-idle'}`;
    badge.textContent = String(stats.completed ?? stats.started ?? 0);
    badge.title = `started ${stats.started ?? 0}, completed ${stats.completed ?? 0}, active ${stats.active ?? 0}`;
    return badge;
}

export const markerClasses = MARKERS;

/** Test hook only: lets the harness count live instances after disposal. */
export const _ModelerPrototype = Modeler.prototype;
