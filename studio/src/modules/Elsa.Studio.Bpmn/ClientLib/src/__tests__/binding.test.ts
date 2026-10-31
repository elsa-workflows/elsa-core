import { describe, expect, it } from 'vitest';
import { extensionValuesWithBinding, isBindable, readBinding, type ModdleElement } from '../binding.js';
import { createModdle, document, elementOf } from './moddle.js';

const BOUND = document(`
    <bpmn:serviceTask id="Task_1">
      <bpmn:extensionElements>
        <vendor:before level="1" />
        <elsa:activityBinding activityType="Elsa.WriteLine">
          <elsa:input name="text">{"typeName":"String","expression":{"type":"Literal","value":"a &lt; b &amp;&amp; c"}}</elsa:input>
        </elsa:activityBinding>
        <vendor:after level="2" />
      </bpmn:extensionElements>
    </bpmn:serviceTask>`);

async function rewrite(xml: string, id: string, binding: Parameters<typeof extensionValuesWithBinding>[2]): Promise<string> {
    const moddle = createModdle();
    const { definitions, element } = await elementOf(moddle, xml, id);
    const values = extensionValuesWithBinding(moddle, element, binding);
    const extensionElements = element.extensionElements as ModdleElement | undefined;

    if (extensionElements) extensionElements.values = values;
    else element.extensionElements = moddle.create('bpmn:ExtensionElements', { values });

    const { xml: saved } = await moddle.toXML(definitions);
    return saved;
}

describe('readBinding', () => {
    it('reads the activity type and each input as its JSON text, unescaped', async () => {
        const { element } = await elementOf(createModdle(), BOUND, 'Task_1');

        expect(readBinding(element)).toEqual({
            activityType: 'Elsa.WriteLine',
            inputs: [{ name: 'text', value: '{"typeName":"String","expression":{"type":"Literal","value":"a < b && c"}}' }]
        });
    });

    it('returns null for an element without a binding', async () => {
        const { element } = await elementOf(createModdle(), document('<bpmn:task id="Task_1" />'), 'Task_1');

        expect(readBinding(element)).toBeNull();
    });
});

describe('isBindable', () => {
    const cases: [string, string, boolean][] = [
        ['a task', '<bpmn:task id="E" />', true],
        ['a user task', '<bpmn:userTask id="E" />', true],
        ['a service task', '<bpmn:serviceTask id="E" />', true],
        ['a script task', '<bpmn:scriptTask id="E" />', true],
        ['a manual task', '<bpmn:manualTask id="E" />', true],
        ['a business rule task', '<bpmn:businessRuleTask id="E" />', true],
        ['a send task without a message', '<bpmn:sendTask id="E" />', true],
        ['a receive task without a message', '<bpmn:receiveTask id="E" />', true],
        ['a send task that names a message', '<bpmn:sendTask id="E" messageRef="Message_1" />', false],
        ['a receive task that names a message', '<bpmn:receiveTask id="E" messageRef="Message_1" />', false],
        ['a gateway', '<bpmn:exclusiveGateway id="E" />', false],
        ['an event', '<bpmn:startEvent id="E" />', false],
        ['a subprocess', '<bpmn:subProcess id="E" />', false],
        ['a call activity', '<bpmn:callActivity id="E" calledElement="Other" />', false]
    ];

    it.each(cases)('%s: %s', async (_, content, expected) => {
        const xml = document(content).replace('<bpmn:process', '<bpmn:message id="Message_1" name="M" />\n  <bpmn:process');
        const { element } = await elementOf(createModdle(), xml, 'E');

        expect(isBindable(element)).toBe(expected);
    });
});

describe('extensionValuesWithBinding', () => {
    it('replaces the binding where it stood and keeps the foreign extension content around it', async () => {
        const saved = await rewrite(BOUND, 'Task_1', { activityType: 'Elsa.SetVariable', inputs: [{ name: 'value', value: '{"a":1}' }] });

        expect(saved).toMatch(/<vendor:before level="1" \/>\s*<elsa:activityBinding activityType="Elsa.SetVariable">\s*<elsa:input name="value">\{"a":1\}<\/elsa:input>\s*<\/elsa:activityBinding>\s*<vendor:after level="2" \/>/);
        expect(saved).not.toContain('Elsa.WriteLine');
    });

    it('removes the binding and keeps the rest for null', async () => {
        const saved = await rewrite(BOUND, 'Task_1', null);

        expect(saved).not.toContain('elsa:activityBinding');
        expect(saved).toMatch(/<vendor:before level="1" \/>\s*<vendor:after level="2" \/>/);
    });

    it('adds a binding to an element that has no extension elements, escaping its JSON as XML text', async () => {
        const saved = await rewrite(document('<bpmn:task id="Task_1" />'), 'Task_1', {
            activityType: 'Elsa.WriteLine',
            inputs: [{ name: 'text', value: '{"value":"a < b && c"}' }]
        });

        expect(saved).toContain('<elsa:input name="text">{"value":"a &lt; b &amp;&amp; c"}</elsa:input>');

        const { element } = await elementOf(createModdle(), saved, 'Task_1');
        expect(readBinding(element)?.inputs[0].value).toBe('{"value":"a < b && c"}');
    });

    it('keeps only the replacement when the element carried more than one binding', async () => {
        const twice = BOUND.replace('<vendor:after level="2" />', '<elsa:activityBinding activityType="Elsa.Second" />\n        <vendor:after level="2" />');

        const saved = await rewrite(twice, 'Task_1', { activityType: 'Elsa.Third', inputs: [] });

        expect(saved.match(/elsa:activityBinding activityType/g)).toHaveLength(1);
        expect(saved).toContain('Elsa.Third');
    });

    it('does not change the element', async () => {
        const moddle = createModdle();
        const { element } = await elementOf(moddle, BOUND, 'Task_1');

        extensionValuesWithBinding(moddle, element, null);

        expect(readBinding(element)?.activityType).toBe('Elsa.WriteLine');
    });

    it.each([
        ['no activity type', { activityType: '', inputs: [] }],
        ['an input named twice', { activityType: 'Elsa.WriteLine', inputs: [{ name: 'text', value: '1' }, { name: 'text', value: '2' }] }]
    ])('refuses a binding with %s, as Elsa\'s import does', async (_, binding) => {
        const moddle = createModdle();
        const { element } = await elementOf(moddle, BOUND, 'Task_1');

        expect(() => extensionValuesWithBinding(moddle, element, binding)).toThrow(TypeError);
    });
});
