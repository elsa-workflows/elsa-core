// @vitest-environment jsdom
// The contract's load, save, binding, undo and dirty rules, as studio/test/browser/BpmnDesigner/contract.spec.ts checks
// them against a designer, checked here against the session a designer delegates them to.
import { describe, expect, it } from 'vitest';
import type { BpmnChangeEvent } from '../contract.js';
import { BpmnModelingSession, type BpmnModelingSessionOptions } from '../modeling/index.js';
import { fixture } from './modeling-fixtures.js';

const CHARGE_BINDING = { activityType: 'Elsa.WriteLine', inputs: [{ name: 'Text', value: '"Charging"' }] };

async function loaded(file: string, options?: BpmnModelingSessionOptions) {
    const session = new BpmnModelingSession(options);
    const result = await session.load(fixture(file));
    const changes: BpmnChangeEvent[] = [];
    session.onChange(change => changes.push(change));
    return { session, result, changes };
}

describe('load', () => {
    it('loads a document with no warnings and raises no change', async () => {
        const { result, changes, session } = await loaded('order.bpmn');

        expect(result).toEqual({ imported: true, warnings: [] });
        expect(changes).toEqual([]);
        expect(session.isDirty).toBe(false);
    });

    it('reports a document it cannot read', async () => {
        const result = await new BpmnModelingSession().load('<bpmn:definitions');

        expect(result).toEqual({ imported: false, error: expect.any(String), warnings: [] });
    });

    it('keeps the document, its history and its dirty state when a load fails', async () => {
        const { session } = await loaded('order.bpmn');
        const edited = { activityType: 'Elsa.Delay', inputs: [] };
        session.setBinding('Charge', edited);

        expect((await session.load('<bpmn:definitions')).imported).toBe(false);

        expect(session.getBinding('Charge')).toEqual(edited);
        expect(session.isDirty).toBe(true);
        expect(session.undo()).toBe(true);
        expect(session.getBinding('Charge')).toEqual(CHARGE_BINDING);
    });

    it('clears the history of the document it replaces', async () => {
        const { session } = await loaded('order.bpmn');
        session.setBinding('Review', CHARGE_BINDING);

        await session.load(fixture('order.bpmn'));

        expect(session.isDirty).toBe(false);
        expect(session.undo()).toBe(false);
    });
});

describe('save', () => {
    it('throws while the session has no document', async () => {
        const session = new BpmnModelingSession();
        await session.load('<bpmn:definitions');

        await expect(session.save()).rejects.toThrow();
    });

    it('keeps bindings and foreign extension content in place, and loads again with no warnings', async () => {
        const { session } = await loaded('order.bpmn');

        const { xml } = await session.save();

        expect(xml).toMatch(/<vendor:retry count="3"\s*\/>\s*<elsa:activityBinding activityType="Elsa.WriteLine">\s*<elsa:input name="Text">"Charging"<\/elsa:input>\s*<\/elsa:activityBinding>\s*<vendor:audit level="full"\s*\/>/);
        expect(await new BpmnModelingSession().load(xml)).toEqual({ imported: true, warnings: [] });
    });
});

describe('bindings', () => {
    it('getBinding reads a binding, and returns null for an element without one or an unknown element', async () => {
        const { session } = await loaded('order.bpmn');

        expect(session.getBinding('Charge')).toEqual(CHARGE_BINDING);
        expect(session.getBinding('Review')).toBeNull();
        expect(session.getBinding('Nope')).toBeNull();
    });

    it('setBinding replaces a binding in place as one undo step, and raises change', async () => {
        const { session, changes } = await loaded('order.bpmn');
        const binding = { activityType: 'Elsa.Http.SendHttpRequest', inputs: [{ name: 'Url', value: '"https://example.com"' }] };

        expect(session.setBinding('Charge', binding)).toEqual({ applied: true });

        expect(session.getBinding('Charge')).toEqual(binding);
        expect((await session.save()).xml).toMatch(/<vendor:retry count="3"\s*\/>\s*<elsa:activityBinding activityType="Elsa.Http.SendHttpRequest">[\s\S]*?<\/elsa:activityBinding>\s*<vendor:audit level="full"\s*\/>/);
        expect(changes).toEqual([{ elementIds: ['Charge'], isDirty: true, canUndo: true, canRedo: false }]);

        expect(session.undo()).toBe(true);
        expect(session.getBinding('Charge')).toEqual(CHARGE_BINDING);
        expect(session.redo()).toBe(true);
        expect(session.getBinding('Charge')).toEqual(binding);
    });

    it('setBinding adds a binding to an element without extension content, and removes it again', async () => {
        const { session } = await loaded('order.bpmn');

        expect(session.setBinding('Review', CHARGE_BINDING)).toEqual({ applied: true });
        expect(session.getBinding('Review')).toEqual(CHARGE_BINDING);
        expect(session.setBinding('Review', null)).toEqual({ applied: true });
        expect(session.getBinding('Review')).toBeNull();

        expect((await session.save()).xml).toMatch(/<bpmn:userTask id="Review" name="Review order">\s*<bpmn:incoming>/);
    });

    it('setBinding removing a binding keeps the foreign extension content', async () => {
        const { session } = await loaded('order.bpmn');

        expect(session.setBinding('Charge', null)).toEqual({ applied: true });

        expect((await session.save()).xml).toMatch(/<bpmn:extensionElements>\s*<vendor:retry count="3"\s*\/>\s*<vendor:audit level="full"\s*\/>\s*<\/bpmn:extensionElements>/);
    });

    it('setBinding removing the last binding keeps foreign attributes of the extension content, and undo puts it back', async () => {
        const { session } = await loaded('foreign-attributes.bpmn');
        const original = (await session.save()).xml;

        expect(session.setBinding('Archive', null)).toEqual({ applied: true });

        const { xml } = await session.save();
        expect(xml).toMatch(/<bpmn:extensionElements [^>]*vendor:origin="import"/);
        expect(xml).not.toContain('elsa:activityBinding');
        expect(session.undo()).toBe(true);
        expect((await session.save()).xml).toBe(original);
    });

    it('setBinding with nothing to remove applies without an edit', async () => {
        const { session, changes } = await loaded('order.bpmn');

        expect(session.setBinding('Review', null)).toEqual({ applied: true });

        expect(changes).toEqual([]);
        expect(session.undo()).toBe(false);
    });

    it('setBinding removing a binding already removed keeps the foreign extension content without another edit', async () => {
        const { session, changes } = await loaded('order.bpmn');
        session.setBinding('Charge', null);
        changes.length = 0;

        expect(session.setBinding('Charge', null)).toEqual({ applied: true });

        expect(changes).toEqual([]);
        expect(session.undo()).toBe(true);
        expect(session.undo()).toBe(false);
    });

    it('setBinding refuses an unknown element, an element that cannot carry a binding, and a message task', async () => {
        const { session, changes } = await loaded('order.bpmn');

        expect(session.setBinding('Nope', CHARGE_BINDING)).toEqual({ applied: false, reason: 'unknown-element' });
        expect(session.setBinding('Charge_di', CHARGE_BINDING)).toEqual({ applied: false, reason: 'unknown-element' });
        expect(session.setBinding('Gateway', CHARGE_BINDING)).toEqual({ applied: false, reason: 'not-bindable' });
        expect(session.setBinding('Start', CHARGE_BINDING)).toEqual({ applied: false, reason: 'not-bindable' });
        expect(session.setBinding('Notify', CHARGE_BINDING)).toEqual({ applied: false, reason: 'not-bindable' });
        expect(changes).toEqual([]);
    });

    it('setBinding refuses every element while no document is loaded', () => {
        expect(new BpmnModelingSession().setBinding('Charge', CHARGE_BINDING)).toEqual({ applied: false, reason: 'unknown-element' });
    });

    it('setBinding throws for a binding Elsa would refuse, and changes nothing', async () => {
        const { session, changes } = await loaded('order.bpmn');

        expect(() => session.setBinding('Review', { activityType: '', inputs: [] })).toThrow(/activity type/);
        expect(() => session.setBinding('Review', { activityType: 'A', inputs: [{ name: 'X', value: '1' }, { name: 'X', value: '2' }] })).toThrow(/twice/);
        expect(changes).toEqual([]);
    });
});

describe('change, undo and redo', () => {
    it('save marks the document clean, and undo past the save makes it dirty again', async () => {
        const { session, changes } = await loaded('order.bpmn');
        session.setBinding('Review', CHARGE_BINDING);
        await session.save();
        changes.length = 0;

        session.undo();
        session.redo();

        expect(changes.map(change => change.isDirty)).toEqual([true, false]);
    });

    it('an edit after an undo is dirty, though the history has the same length', async () => {
        const { session, changes } = await loaded('order.bpmn');
        session.setBinding('Review', CHARGE_BINDING);
        await session.save();
        session.undo();
        changes.length = 0;

        session.setBinding('Review', { activityType: 'Elsa.Other', inputs: [] });

        expect(changes).toEqual([{ elementIds: ['Review'], isDirty: true, canUndo: true, canRedo: false }]);
    });

    it('save marks clean the document it returns, not an edit made while it runs', async () => {
        const { session, changes } = await loaded('order.bpmn');

        const saving = session.save();
        session.setBinding('Review', CHARGE_BINDING);
        const { xml } = await saving;
        changes.length = 0;
        session.undo();
        session.redo();

        expect(xml).not.toMatch(/<bpmn:userTask id="Review"[^>]*>\s*<bpmn:extensionElements>/);
        expect(changes.map(change => change.isDirty)).toEqual([false, true]);
    });

    it('a save that a load overtakes leaves the newly loaded document clean', async () => {
        const { session } = await loaded('order.bpmn');
        session.setBinding('Review', CHARGE_BINDING);
        // Hold the save's write open until the load has replaced the document.
        const saved = session.document!;
        const write = saved.write.bind(saved);
        let release!: () => void;
        saved.write = () => write().then(xml => new Promise<string>(resolve => (release = () => resolve(xml))));

        const saving = session.save();
        await session.load(fixture('order.bpmn'));
        release();
        await saving;

        expect(session.isDirty).toBe(false);
    });

    it('undo and redo return false when there is nothing to do', async () => {
        const { session } = await loaded('order.bpmn');

        expect(session.undo()).toBe(false);
        expect(session.redo()).toBe(false);
    });

    it('an unsubscribed listener hears no more changes', async () => {
        const { session } = await loaded('order.bpmn');
        const heard: BpmnChangeEvent[] = [];
        const unsubscribe = session.onChange(change => heard.push(change));

        unsubscribe();
        session.setBinding('Review', CHARGE_BINDING);

        expect(heard).toEqual([]);
    });
});

describe('read-only', () => {
    it('loads and saves, but refuses every edit', async () => {
        const { session, result, changes } = await loaded('order.bpmn', { readOnly: true });

        expect(result).toEqual({ imported: true, warnings: [] });
        expect(session.getBinding('Charge')).toEqual(CHARGE_BINDING);
        expect(session.setBinding('Charge', null)).toEqual({ applied: false, reason: 'read-only' });
        expect(session.undo()).toBe(false);
        expect(() => session.execute({ elementIds: [], execute() {}, revert() {} })).toThrow(/read-only/);
        expect((await session.save()).xml).toContain('elsa:activityBinding');
        expect(changes).toEqual([]);
    });
});
