/**
 * SPIKE (#8901) -- a minimal properties-panel provider for the `elsa:` vendor extension.
 *
 * Adds an "Elsa activity" group (the `activityType` of `elsa:activityBinding`) and an "Elsa inputs" list group (one
 * item per `elsa:input`, name plus raw JSON text) to tasks. Only tasks: Elsa's binder refuses a binding on anything
 * that is not an unbound task (a gateway binding is refused at import). Every write goes through the command stack,
 * so it is undoable and raises the modeler's change event. A real provider would replace the free-text activity type
 * with Studio's activity picker and the JSON text area with Studio's input editors (hosted outside the module).
 */
import { ListGroup, TextAreaEntry, TextFieldEntry, isTextAreaEntryEdited, isTextFieldEntryEdited } from '@bpmn-io/properties-panel';
import { useService } from 'bpmn-js-properties-panel';
import { getBusinessObject, is } from 'bpmn-js/lib/util/ModelUtil';

const LOW_PRIORITY = 500;
const BINDING_TYPE = 'elsa:ActivityBinding';

export function findBinding(businessObject) {
    const values = businessObject.get('extensionElements')?.get('values') ?? [];
    return values.find(value => is(value, BINDING_TYPE));
}

function isBindable(element) {
    return is(element, 'bpmn:Task');
}

function ElsaPropertiesProvider(propertiesPanel, injector) {
    this.getGroups = element => groups => {
        if (!isBindable(element)) return groups;
        groups.push({
            id: 'elsa-activity',
            label: 'Elsa activity',
            entries: [{ id: 'elsa-activityType', component: ActivityTypeEntry, isEdited: isTextFieldEntryEdited }]
        });
        const binding = findBinding(getBusinessObject(element));
        if (binding) groups.push(inputsGroup(element, binding, injector));
        return groups;
    };

    propertiesPanel.registerProvider(LOW_PRIORITY, this);
}

ElsaPropertiesProvider.$inject = ['propertiesPanel', 'injector'];

function ActivityTypeEntry({ element }) {
    const bpmnFactory = useService('bpmnFactory');
    const commandStack = useService('commandStack');
    const debounce = useService('debounceInput');
    const businessObject = getBusinessObject(element);

    return TextFieldEntry({
        element,
        id: 'elsa-activityType',
        label: 'Activity type',
        description: 'The Elsa activity type that performs this element, e.g. Elsa.WriteLine.',
        getValue: () => findBinding(businessObject)?.get('activityType') ?? '',
        setValue: value => setActivityType(element, businessObject, value, bpmnFactory, commandStack),
        debounce
    });
}

function setActivityType(element, businessObject, value, bpmnFactory, commandStack) {
    const binding = findBinding(businessObject);
    let extensionElements = businessObject.get('extensionElements');

    if (!value) {
        if (binding) {
            commandStack.execute('element.updateModdleProperties', {
                element,
                moddleElement: extensionElements,
                properties: { values: extensionElements.get('values').filter(candidate => candidate !== binding) }
            });
        }
        return;
    }

    if (binding) {
        commandStack.execute('element.updateModdleProperties', { element, moddleElement: binding, properties: { activityType: value } });
        return;
    }

    const commands = [];
    if (!extensionElements) {
        extensionElements = bpmnFactory.create('bpmn:ExtensionElements', { values: [] });
        extensionElements.$parent = businessObject;
        commands.push({ cmd: 'element.updateModdleProperties', context: { element, moddleElement: businessObject, properties: { extensionElements } } });
    }
    const newBinding = bpmnFactory.create(BINDING_TYPE, { activityType: value, inputs: [] });
    newBinding.$parent = extensionElements;
    commands.push({
        cmd: 'element.updateModdleProperties',
        context: { element, moddleElement: extensionElements, properties: { values: [...extensionElements.get('values'), newBinding] } }
    });
    commandStack.execute('properties-panel.multi-command-executor', commands);
}

function inputsGroup(element, binding, injector) {
    const bpmnFactory = injector.get('bpmnFactory');
    const commandStack = injector.get('commandStack');

    const items = binding.get('inputs').map((input, index) => {
        const id = `${element.id}-elsa-input-${index}`;
        return {
            id,
            label: input.get('name') || '<unnamed>',
            entries: [
                { id: `${id}-name`, component: InputNameEntry, input, idPrefix: id, isEdited: isTextFieldEntryEdited },
                { id: `${id}-value`, component: InputValueEntry, input, idPrefix: id, isEdited: isTextAreaEntryEdited }
            ],
            autoFocusEntry: `${id}-name`,
            remove: event => {
                event.stopPropagation();
                commandStack.execute('element.updateModdleProperties', {
                    element,
                    moddleElement: binding,
                    properties: { inputs: binding.get('inputs').filter(candidate => candidate !== input) }
                });
            }
        };
    });

    return {
        id: 'elsa-inputs',
        label: 'Elsa inputs',
        component: ListGroup,
        items,
        add: event => {
            event.stopPropagation();
            const input = bpmnFactory.create('elsa:Input', { name: '', value: '' });
            input.$parent = binding;
            commandStack.execute('element.updateModdleProperties', {
                element,
                moddleElement: binding,
                properties: { inputs: [...binding.get('inputs'), input] }
            });
        }
    };
}

function InputNameEntry({ element, input, idPrefix }) {
    const commandStack = useService('commandStack');
    const debounce = useService('debounceInput');
    return TextFieldEntry({
        element: input,
        id: `${idPrefix}-name`,
        label: 'Name',
        getValue: () => input.get('name') ?? '',
        setValue: value => commandStack.execute('element.updateModdleProperties', { element, moddleElement: input, properties: { name: value } }),
        debounce
    });
}

function InputValueEntry({ element, input, idPrefix }) {
    const commandStack = useService('commandStack');
    const debounce = useService('debounceInput');
    return TextAreaEntry({
        element: input,
        id: `${idPrefix}-value`,
        label: 'Value (JSON, as Elsa\'s activity serializer writes it)',
        monospace: true,
        rows: 4,
        getValue: () => input.get('value') ?? '',
        setValue: value => commandStack.execute('element.updateModdleProperties', { element, moddleElement: input, properties: { value } }),
        validate: value => {
            if (!value) return null;
            try {
                JSON.parse(value);
                return null;
            } catch {
                return 'Not valid JSON.';
            }
        },
        debounce
    });
}

export default {
    __init__: ['elsaPropertiesProvider'],
    elsaPropertiesProvider: ['type', ElsaPropertiesProvider]
};
