/**
 * The BPMN designer contract, version 1: what every Elsa Studio BPMN designer implements, so the code around a designer
 * does not depend on which one the host enabled.
 *
 * Specified in studio/docs/specs/bpmn-designer-contract/contract.md. This file and that document change together.
 */

/** The contract version this file describes. */
export const BPMN_DESIGNER_CONTRACT_VERSION = 1;

/** What a designer module exports by default. */
export interface BpmnDesignerModule {
    /** A stable id hosts select the designer by, such as `bpmn-js` or `x6`. */
    readonly designerId: string;
    /** The contract version the module implements. */
    readonly contractVersion: number;
    /** Creates a designer that draws into `container`. */
    createDesigner(container: HTMLElement, options?: BpmnDesignerOptions): BpmnDesigner;
}

export interface BpmnDesignerOptions {
    /** When set, nothing the user does changes the document, and `setBinding` refuses with `read-only`. */
    readOnly?: boolean;
    /** Where the designer may render its own properties panel. A designer may ignore it. */
    propertiesContainer?: HTMLElement | null;
}

/** A designer instance. Elements are identified by their BPMN element id. Every method but `dispose` throws after `dispose`. */
export interface BpmnDesigner {
    load(xml: string): Promise<BpmnLoadResult>;
    save(): Promise<BpmnSaveResult>;
    select(elementId: string | null): boolean;
    getBinding(elementId: string): BpmnActivityBinding | null;
    setBinding(elementId: string, binding: BpmnActivityBinding | null): BpmnSetBindingResult;
    /** Undoes the last edit. Returns `false` when there is nothing to undo, and always in a read-only designer. */
    undo(): boolean;
    /** Redoes the last undone edit. Returns `false` when there is nothing to redo, and always in a read-only designer. */
    redo(): boolean;
    applyOverlays(statsById: Record<string, BpmnElementStats> | null): BpmnOverlayResult;
    clearOverlays(): BpmnOverlayResult;
    on(event: 'change', listener: (event: BpmnChangeEvent) => void): () => void;
    on(event: 'selection', listener: (event: BpmnSelectionEvent) => void): () => void;
    dispose(): void;
}

/** `imported` is `false` when the document could not be shown at all; `error` then says why. */
export type BpmnLoadResult =
    | { imported: true; warnings: BpmnLoadWarning[] }
    | { imported: false; error: string; warnings: BpmnLoadWarning[] };

/**
 * `dropped`: content a save would lose. `rewritten`: content a save keeps but writes differently. A later designer may
 * report a kind added since, so a host treats a kind it does not know as `dropped`.
 */
export type BpmnLoadWarningKind = 'dropped' | 'rewritten' | (string & {});

export interface BpmnLoadWarning {
    kind: BpmnLoadWarningKind;
    message: string;
    elementId?: string;
}

export interface BpmnSaveResult {
    xml: string;
}

/** An element's `elsa:activityBinding`. Each input value is that input's JSON text, as the document stores it. */
export interface BpmnActivityBinding {
    activityType: string;
    inputs: BpmnActivityBindingInput[];
}

export interface BpmnActivityBindingInput {
    name: string;
    value: string;
}

export type BpmnSetBindingRefusal = 'unknown-element' | 'not-bindable' | 'read-only';

export type BpmnSetBindingResult = { applied: true } | { applied: false; reason: BpmnSetBindingRefusal };

/**
 * Instance state for one element, keyed by its BPMN id. The same shape as the Designer ClientLib's `BpmnElementStats`
 * (src/bpmn/model.ts) and Studio's C# `BpmnElementStats`. Every field is optional: what a projection cannot compute, it
 * leaves out.
 */
export interface BpmnElementStats {
    readonly started?: number;
    readonly completed?: number;
    readonly active?: number;
    readonly blocked?: boolean;
    readonly faulted?: boolean;
    readonly canceled?: boolean;
}

export interface BpmnOverlayResult {
    applied: string[];
    unknownElementIds: string[];
}

export interface BpmnChangeEvent {
    elementIds: string[];
    isDirty: boolean;
    canUndo: boolean;
    canRedo: boolean;
}

export interface BpmnSelectionEvent {
    elementIds: string[];
    primary: { id: string; type: string; name?: string } | null;
}
