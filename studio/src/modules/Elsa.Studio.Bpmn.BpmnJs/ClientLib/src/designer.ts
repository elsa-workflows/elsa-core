/**
 * The bpmn-js BPMN designer: BPMN designer contract version 1 on bpmn-js.
 *
 * Elements are identified by their BPMN element id everywhere: selection, bindings, change events and overlays. A
 * designer created with `readOnly` is a bpmn-js viewer, so it has no editing tools at all.
 */
import Modeler from 'bpmn-js/lib/Modeler';
import NavigatedViewer from 'bpmn-js/lib/NavigatedViewer';
import { BpmnPropertiesPanelModule, BpmnPropertiesProviderModule } from 'bpmn-js-properties-panel';
import {
    computeLoadWarnings,
    elsaModdleDescriptor,
    extensionValuesWithBinding,
    isBindable,
    readBinding,
    withGeneratedLayout,
    type BpmnActivityBinding,
    type BpmnChangeEvent,
    type BpmnDesigner,
    type BpmnDesignerOptions,
    type BpmnElementStats,
    type BpmnLoadResult,
    type BpmnOverlayResult,
    type BpmnSaveResult,
    type BpmnSelectionEvent,
    type BpmnSetBindingResult,
    type ModdleElement,
    type ModdleFactory
} from '../../../Elsa.Studio.Bpmn/ClientLib/src/index.js';

/** The part of a bpmn-js shape or connection the designer reads. */
interface DiagramElement {
    readonly id: string;
    readonly type: string;
    readonly businessObject?: ModdleElement;
    readonly labelTarget?: unknown;
}

interface ElementRegistry {
    get(id: string): DiagramElement | undefined;
    find(predicate: (element: DiagramElement) => boolean): DiagramElement | undefined;
}

interface Modeling {
    updateProperties(element: DiagramElement, properties: Record<string, unknown>): void;
    updateModdleProperties(element: DiagramElement, moddleElement: ModdleElement, properties: Record<string, unknown>): void;
}

type CommandStackTrigger = 'execute' | 'undo' | 'redo' | 'clear' | null;

const OVERLAY_TYPE = 'elsa-element-state';
const CONTAINER_CLASS = 'elsa-bpmn-js-designer';

export function createDesigner(container: HTMLElement, options: BpmnDesignerOptions = {}): BpmnDesigner {
    if (!(container instanceof HTMLElement)) throw new TypeError('A BPMN designer needs a container element.');

    const readOnly = options.readOnly === true;
    const propertiesContainer = !readOnly && options.propertiesContainer instanceof HTMLElement ? options.propertiesContainer : null;
    const moddleExtensions = { elsa: elsaModdleDescriptor };
    let bpmnjs: Modeler | NavigatedViewer | null = readOnly
        ? new NavigatedViewer({ container, moddleExtensions })
        : new Modeler({
            container,
            moddleExtensions,
            propertiesPanel: propertiesContainer ? { parent: propertiesContainer } : undefined,
            additionalModules: propertiesContainer ? [BpmnPropertiesPanelModule, BpmnPropertiesProviderModule] : []
        });

    container.classList.add(CONTAINER_CLASS);

    const listeners = {
        change: new Set<(event: BpmnChangeEvent) => void>(),
        selection: new Set<(event: BpmnSelectionEvent) => void>()
    };
    const eventBus = service<{ on(event: string, priority: number, callback: (event: any) => void): void; off(event: string, callback: (event: any) => void): void }>('eventBus');
    const unsubscribers: (() => void)[] = [];
    const markedElements = new Map<string, string>();
    let changedIds = new Set<string>();
    let loading = false;
    let hasDocument = false;
    let disposed = false;

    // Dirty tracking counts commands rather than reading the command stack's private index. Each executed command gets
    // a new state id; undo and redo move along the same history, so the document is clean exactly when the current
    // state is the one last saved, even after an undo followed by a different edit.
    let history: number[] = [];
    let position = 0;
    let nextStateId = 1;
    let savedStateId = 0;
    const currentStateId = () => (position === 0 ? 0 : history[position - 1]);

    function service<T>(name: string): T {
        if (!bpmnjs) throw new Error('This BPMN designer has been disposed.');
        return bpmnjs.get(name) as T;
    }

    function subscribe(event: string, callback: (event: any) => void): void {
        eventBus.on(event, 1000, callback);
        unsubscribers.push(() => eventBus.off(event, callback));
    }

    function emit(kind: 'change', payload: BpmnChangeEvent): void;
    function emit(kind: 'selection', payload: BpmnSelectionEvent): void;
    function emit(kind: keyof typeof listeners, payload: unknown): void {
        for (const listener of [...listeners[kind]] as ((payload: unknown) => void)[]) {
            try {
                listener(payload);
            } catch (error) {
                console.error(`elsa-bpmn-js-designer: a ${kind} listener threw.`, error);
            }
        }
    }

    function idOf(element: DiagramElement): string {
        return (element.businessObject?.id as string | undefined) ?? element.id;
    }

    subscribe('elements.changed', (event: { elements: DiagramElement[] }) => {
        for (const element of event.elements) {
            if (!element.labelTarget) changedIds.add(idOf(element));
        }
    });

    subscribe('commandStack.changed', (event: { trigger: CommandStackTrigger }) => {
        switch (event.trigger) {
            case 'execute':
                history = [...history.slice(0, position), nextStateId++];
                position = history.length;
                break;
            case 'undo':
                position = Math.max(0, position - 1);
                break;
            case 'redo':
                position = Math.min(history.length, position + 1);
                break;
            case 'clear':
                history = [];
                position = 0;
                break;
        }

        if (!loading) emitChange();
    });

    function emitChange(): void {
        // A read-only designer is a NavigatedViewer, which has no command stack: it can never undo or redo.
        const commandStack = readOnly ? null : service<{ canUndo(): boolean; canRedo(): boolean }>('commandStack');
        const elementIds = [...changedIds];
        changedIds = new Set();
        emit('change', { elementIds, isDirty: currentStateId() !== savedStateId, canUndo: commandStack?.canUndo() ?? false, canRedo: commandStack?.canRedo() ?? false });
    }

    subscribe('selection.changed', (event: { newSelection: DiagramElement[] }) => {
        // Importing clears the selection, several times. The host learns about a new document from load's result.
        if (loading) return;
        const selected = event.newSelection.filter(element => !element.labelTarget);
        const primary = selected[0];
        const name = primary?.businessObject?.name as string | undefined;
        emit('selection', {
            elementIds: selected.map(idOf),
            primary: primary ? { id: idOf(primary), type: primary.type, ...(name ? { name } : {}) } : null
        });
    });

    function findElement(elementId: string): DiagramElement | undefined {
        const registry = service<ElementRegistry>('elementRegistry');
        const direct = registry.get(elementId);
        if (direct && !direct.labelTarget) return direct;
        return registry.find(element => !element.labelTarget && element.businessObject?.id === elementId);
    }

    function ensureLive(): void {
        if (disposed) throw new Error('This BPMN designer has been disposed.');
    }

    /**
     * Puts back the document a failed load replaced, and whether it was dirty. Its undo history is gone: bpmn-js clears
     * it on import. With no document to put back, the designer holds none.
     */
    async function restore(previous: { xml: string; isDirty: boolean } | null): Promise<void> {
        try {
            if (!previous) throw new Error('There is no earlier document.');
            await bpmnjs!.importXML(previous.xml);
        } catch {
            // Without an earlier document, or if it no longer imports, the designer holds no document.
            bpmnjs!.clear();
            hasDocument = false;
            return;
        }
        // A state id no command gets, so a dirty document stays dirty until it is saved.
        savedStateId = previous.isDirty ? -1 : currentStateId();
        changedIds = new Set();
        // The import cleared the undo history. A read-only designer never had one, so nothing changed for it.
        if (!readOnly) emitChange();
    }

    async function serialize(): Promise<string> {
        const { xml } = await bpmnjs!.saveXML({ format: true });
        if (xml === undefined) throw new Error('bpmn-js produced no XML.');
        return xml;
    }

    // Methods never use `this`: a JS interop host may call them detached from the designer.
    function clearOverlays(): BpmnOverlayResult {
        ensureLive();
        service<{ remove(filter: { type: string }): void }>('overlays').remove({ type: OVERLAY_TYPE });
        const canvas = service<{ removeMarker(element: DiagramElement, marker: string): void }>('canvas');
        for (const [shapeId, marker] of markedElements) {
            const element = service<ElementRegistry>('elementRegistry').get(shapeId);
            if (element) canvas.removeMarker(element, marker);
        }
        markedElements.clear();
        return { applied: [], unknownElementIds: [] };
    }

    return {
        async load(xml: string): Promise<BpmnLoadResult> {
            ensureLive();
            loading = true;
            const definitions = bpmnjs!.getDefinitions();
            let previous: { xml: string; isDirty: boolean } | null = null;
            try {
                // bpmn-js can replace the document before an import fails, so keep what a save would write now.
                if (hasDocument) previous = { xml: await serialize(), isDirty: currentStateId() !== savedStateId };
                clearOverlays();
                const layout = await withGeneratedLayout(xml);
                await bpmnjs!.importXML(layout.xml);
                // What a save right after loading writes, compared with what the host gave us.
                const warnings = [...computeLoadWarnings(xml, await serialize()), ...layout.warnings];
                savedStateId = currentStateId();
                changedIds = new Set();
                service<{ zoom(level: string, center: string): void }>('canvas').zoom('fit-viewport', 'auto');
                hasDocument = true;
                return { imported: true, warnings };
            } catch (error) {
                if (bpmnjs!.getDefinitions() !== definitions) await restore(previous);
                return { imported: false, error: error instanceof Error ? error.message : String(error), warnings: [] };
            } finally {
                loading = false;
            }
        },

        async save(): Promise<BpmnSaveResult> {
            ensureLive();
            if (!hasDocument) throw new Error('No document is loaded.');
            // An edit made while the export runs is not in the XML, so only the state being exported becomes clean.
            const exportedStateId = currentStateId();
            const xml = await serialize();
            savedStateId = exportedStateId;
            return { xml };
        },

        select(elementId: string | null): boolean {
            ensureLive();
            const selection = service<{ select(element: DiagramElement | null): void }>('selection');
            if (elementId === null) {
                selection.select(null);
                return true;
            }
            const element = findElement(elementId);
            if (!element) return false;
            selection.select(element);
            service<{ scrollToElement(element: DiagramElement): void }>('canvas').scrollToElement(element);
            return true;
        },

        getBinding(elementId: string): BpmnActivityBinding | null {
            ensureLive();
            const businessObject = findElement(elementId)?.businessObject;
            return businessObject ? readBinding(businessObject) : null;
        },

        setBinding(elementId: string, binding: BpmnActivityBinding | null): BpmnSetBindingResult {
            ensureLive();
            if (readOnly) return { applied: false, reason: 'read-only' };
            const element = findElement(elementId);
            const businessObject = element?.businessObject;
            if (!element || !businessObject) return { applied: false, reason: 'unknown-element' };
            if (!isBindable(businessObject)) return { applied: false, reason: 'not-bindable' };

            const moddle = service<ModdleFactory>('moddle');
            const values = extensionValuesWithBinding(moddle, businessObject, binding);
            const extensionElements = businessObject.extensionElements as ModdleElement | undefined;
            const modeling = service<Modeling>('modeling');

            // Each branch is one command, so the edit is one undo step. An emptied extensionElements is removed, unless
            // it carries foreign attributes, which the element has to keep.
            if (extensionElements && (values.length > 0 || hasForeignAttributes(extensionElements))) {
                modeling.updateModdleProperties(element, extensionElements, { values });
            } else if (extensionElements) {
                modeling.updateProperties(element, { extensionElements: undefined });
            } else if (values.length > 0) {
                modeling.updateProperties(element, { extensionElements: moddle.create('bpmn:ExtensionElements', { values }) });
            }
            return { applied: true };
        },

        undo(): boolean {
            ensureLive();
            if (readOnly) return false;
            const commandStack = service<{ canUndo(): boolean; undo(): void }>('commandStack');
            if (!commandStack.canUndo()) return false;
            commandStack.undo();
            return true;
        },

        redo(): boolean {
            ensureLive();
            if (readOnly) return false;
            const commandStack = service<{ canRedo(): boolean; redo(): void }>('commandStack');
            if (!commandStack.canRedo()) return false;
            commandStack.redo();
            return true;
        },

        applyOverlays(statsById: Record<string, BpmnElementStats> | null): BpmnOverlayResult {
            clearOverlays();
            const overlays = service<{ add(element: DiagramElement, type: string, overlay: { position: Record<string, number>; html: HTMLElement }): void }>('overlays');
            const canvas = service<{ addMarker(element: DiagramElement, marker: string): void }>('canvas');
            const applied: string[] = [];
            const unknownElementIds: string[] = [];

            for (const [elementId, stats] of Object.entries(statsById ?? {})) {
                const element = findElement(elementId);
                if (!element) {
                    unknownElementIds.push(elementId);
                    continue;
                }
                const state = stateOf(stats);
                if (state !== 'idle') {
                    canvas.addMarker(element, `elsa-${state}`);
                    markedElements.set(element.id, `elsa-${state}`);
                }
                overlays.add(element, OVERLAY_TYPE, { position: { top: -10, right: 10 }, html: badge(stats, state) });
                applied.push(elementId);
            }

            return { applied, unknownElementIds };
        },

        clearOverlays,

        on(event: 'change' | 'selection', listener: ((event: BpmnChangeEvent) => void) | ((event: BpmnSelectionEvent) => void)): () => void {
            ensureLive();
            const set = listeners[event] as Set<unknown> | undefined;
            if (!set) throw new Error(`Unknown BPMN designer event '${String(event)}'.`);
            set.add(listener);
            return () => {
                set.delete(listener);
            };
        },

        dispose(): void {
            if (disposed) return;
            disposed = true;
            for (const unsubscribe of unsubscribers.splice(0)) unsubscribe();
            listeners.change.clear();
            listeners.selection.clear();
            markedElements.clear();
            bpmnjs!.destroy();
            bpmnjs = null;
            container.classList.remove(CONTAINER_CLASS);
            propertiesContainer?.replaceChildren();
        }
    } satisfies BpmnDesigner;
}

function hasForeignAttributes(element: ModdleElement): boolean {
    return Object.keys((element.$attrs as Record<string, unknown> | undefined) ?? {}).some(name => !name.startsWith('xmlns'));
}

type ElementState = 'faulted' | 'blocked' | 'active' | 'canceled' | 'completed' | 'idle';

/**
 * The one state an element's marker shows, most urgent first, in the X6 designer's order. A cancellation outranks
 * blocked and active: the stats keep both after an element is cancelled, but a cancelled element is not running.
 */
function stateOf(stats: BpmnElementStats): ElementState {
    if (stats.faulted) return 'faulted';
    if (stats.canceled) return 'canceled';
    if (stats.blocked) return 'blocked';
    if ((stats.active ?? 0) > 0) return 'active';
    if ((stats.completed ?? 0) > 0) return 'completed';
    return 'idle';
}

/** The number a badge shows for its state, as the X6 designer shows it: a running element counts its active tokens. */
function countOf(stats: BpmnElementStats, state: ElementState): number {
    switch (state) {
        case 'active':
            return stats.active ?? 0;
        case 'completed':
            return stats.completed ?? 0;
        case 'idle':
            return stats.completed ?? stats.started ?? 0;
        default:
            return stats.started ?? stats.completed ?? 0;
    }
}

function badge(stats: BpmnElementStats, state: ElementState): HTMLElement {
    const element = document.createElement('div');
    element.className = `elsa-bpmn-badge elsa-bpmn-badge-${state}`;
    element.textContent = String(countOf(stats, state));
    element.title = `Started ${stats.started ?? 0}, completed ${stats.completed ?? 0}, active ${stats.active ?? 0}`;
    return element;
}
