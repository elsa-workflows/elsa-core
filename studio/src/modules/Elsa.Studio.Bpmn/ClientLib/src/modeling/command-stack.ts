/**
 * Undo, redo and dirty tracking for edits to a document.
 *
 * Dirty tracking follows the bpmn-js designer's: each executed command gets a new state id, and undo and redo move
 * along the same history, so the document is clean exactly when the current state is the one last saved, even after an
 * undo followed by a different edit.
 */

/** One undoable edit. `execute` and `revert` must each leave the document exactly as the other found it. */
export interface Command {
    /** The ids of the elements the edit changes. */
    readonly elementIds: readonly string[];
    execute(): void;
    revert(): void;
}

/** Several commands that undo and redo as one step. */
export function compound(commands: readonly Command[]): Command {
    return {
        elementIds: [...new Set(commands.flatMap(command => command.elementIds))],
        execute: () => commands.forEach(command => command.execute()),
        revert: () => [...commands].reverse().forEach(command => command.revert())
    };
}

export interface CommandStackChange {
    /** The ids of the elements the executed, undone or redone command changed. */
    elementIds: string[];
    isDirty: boolean;
    canUndo: boolean;
    canRedo: boolean;
}

/** A state id no command gets, so a document marked with it stays dirty until it is saved. */
const NEVER_SAVED = -1;

export class CommandStack {
    private commands: Command[] = [];
    private stateIds: number[] = [];
    private position = 0;
    private nextStateId = 1;
    private savedStateId = 0;
    private readonly listeners = new Set<(change: CommandStackChange) => void>();

    get canUndo(): boolean {
        return this.position > 0;
    }

    get canRedo(): boolean {
        return this.position < this.commands.length;
    }

    get isDirty(): boolean {
        return this.stateId !== this.savedStateId;
    }

    /** The state the document is in now. A save that captures it before writing marks exactly that state clean. */
    get stateId(): number {
        return this.position === 0 ? 0 : this.stateIds[this.position - 1];
    }

    /** Executes `command` and makes it the last undo step, dropping whatever could have been redone. */
    execute(command: Command): void {
        command.execute();
        this.commands = [...this.commands.slice(0, this.position), command];
        this.stateIds = [...this.stateIds.slice(0, this.position), this.nextStateId++];
        this.position = this.commands.length;
        this.notify(command);
    }

    undo(): boolean {
        if (!this.canUndo) return false;
        const command = this.commands[this.position - 1];
        command.revert();
        this.position--;
        this.notify(command);
        return true;
    }

    redo(): boolean {
        if (!this.canRedo) return false;
        const command = this.commands[this.position];
        command.execute();
        this.position++;
        this.notify(command);
        return true;
    }

    /** Marks `stateId` as the state last saved. Pass the `stateId` read before writing the document. */
    markSaved(stateId: number = this.stateId): void {
        this.savedStateId = stateId;
    }

    /** Keeps the document dirty until it is next saved, whatever is undone or redone. */
    markNeverSaved(): void {
        this.savedStateId = NEVER_SAVED;
    }

    /** Forgets the history; the current state is the saved one. Raises no change. */
    clear(): void {
        this.commands = [];
        this.stateIds = [];
        this.position = 0;
        this.savedStateId = 0;
    }

    /** Calls `listener` after every executed, undone or redone command. Returns a function that unsubscribes. */
    onChange(listener: (change: CommandStackChange) => void): () => void {
        this.listeners.add(listener);
        return () => this.listeners.delete(listener);
    }

    private notify(command: Command): void {
        const change = { elementIds: [...command.elementIds], isDirty: this.isDirty, canUndo: this.canUndo, canRedo: this.canRedo };
        for (const listener of [...this.listeners]) listener(change);
    }
}
