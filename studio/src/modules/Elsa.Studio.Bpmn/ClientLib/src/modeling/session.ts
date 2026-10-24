/**
 * The document a designer edits, with its undo history: the canvas-independent half of the designer contract. A
 * designer draws the session's document and turns the user's gestures into commands; the session loads, saves, binds,
 * undoes and redoes the way the contract specifies.
 */
import { isBindable, readBinding } from '../binding.js';
import type { BpmnActivityBinding, BpmnChangeEvent, BpmnLoadResult, BpmnSaveResult, BpmnSetBindingResult } from '../contract.js';
import { type Command, CommandStack } from './command-stack.js';
import { setBinding } from './commands.js';
import { BpmnDocument } from './document.js';

export interface BpmnModelingSessionOptions {
    /** When set, the session refuses every edit, and undo and redo do nothing. */
    readOnly?: boolean;
}

export class BpmnModelingSession {
    readonly readOnly: boolean;
    private readonly stack = new CommandStack();
    private current: BpmnDocument | null = null;

    constructor(options: BpmnModelingSessionOptions = {}) {
        this.readOnly = options.readOnly ?? false;
    }

    /** The loaded document, or `null` before a load succeeds. */
    get document(): BpmnDocument | null {
        return this.current;
    }

    get isDirty(): boolean {
        return this.stack.isDirty;
    }

    get canUndo(): boolean {
        return !this.readOnly && this.stack.canUndo;
    }

    get canRedo(): boolean {
        return !this.readOnly && this.stack.canRedo;
    }

    /**
     * Loads `xml` in place of the current document, which becomes clean with no history. A load that fails changes
     * nothing: the session keeps the document, the history and the dirty state it had. Raises no change.
     */
    async load(xml: string): Promise<BpmnLoadResult> {
        try {
            const { document, warnings } = await BpmnDocument.read(xml);
            this.current = document;
            this.stack.clear();
            return { imported: true, warnings };
        } catch (error) {
            return { imported: false, error: error instanceof Error ? error.message : String(error), warnings: [] };
        }
    }

    /**
     * The document as XML. Marks clean the state it writes, so an edit made while the returned promise is pending
     * stays dirty.
     *
     * @throws Error when no document is loaded.
     */
    async save(): Promise<BpmnSaveResult> {
        const document = this.requireDocument();
        // write() serializes before it returns its promise, so the XML is exactly the state captured here.
        const stateId = this.stack.stateId;
        const xml = await document.write();
        // A load that replaced the document while this save ran started a new history; the old state means nothing to it.
        if (this.current === document) this.stack.markSaved(stateId);
        return { xml };
    }

    /** The binding of the element with `elementId`, or `null` when it has none or the document has no such element. */
    getBinding(elementId: string): BpmnActivityBinding | null {
        const element = this.current?.element(elementId);
        return element ? readBinding(element) : null;
    }

    /**
     * Replaces the element's binding with `binding`, or removes it for `null`, as one undo step.
     *
     * @throws TypeError when `binding` names no activity type, or names an input twice; Elsa's import refuses both.
     */
    setBinding(elementId: string, binding: BpmnActivityBinding | null): BpmnSetBindingResult {
        if (this.readOnly) return { applied: false, reason: 'read-only' };
        const element = this.current?.element(elementId);
        if (!this.current || !element) return { applied: false, reason: 'unknown-element' };
        if (!isBindable(element)) return { applied: false, reason: 'not-bindable' };

        const command = setBinding(this.current, element, binding);
        if (command) this.stack.execute(command);
        return { applied: true };
    }

    /**
     * Executes `command` as the last undo step.
     *
     * @throws Error when the session is read-only or has no document.
     */
    execute(command: Command): void {
        if (this.readOnly) throw new Error('The document is read-only.');
        this.requireDocument();
        this.stack.execute(command);
    }

    /** Undoes the last edit. Returns `false` when there is nothing to undo, and always when read-only. */
    undo(): boolean {
        return !this.readOnly && this.stack.undo();
    }

    /** Redoes the last undone edit. Returns `false` when there is nothing to redo, and always when read-only. */
    redo(): boolean {
        return !this.readOnly && this.stack.redo();
    }

    /** Calls `listener` after every edit, undo and redo. Returns a function that unsubscribes. */
    onChange(listener: (event: BpmnChangeEvent) => void): () => void {
        return this.stack.onChange(listener);
    }

    private requireDocument(): BpmnDocument {
        if (!this.current) throw new Error('No document is loaded.');
        return this.current;
    }
}
