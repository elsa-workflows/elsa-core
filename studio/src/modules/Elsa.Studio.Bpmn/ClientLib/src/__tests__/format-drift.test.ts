// The elsa: names here and in BpmnActivityBindingFormat (Elsa.Bpmn.Interchange, mirrored in Elsa.Studio.Workflows.Core)
// are one compatibility surface. A rename on either side alone would make Studio write bindings the server ignores.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import {
    ACTIVITY_TYPE_ATTRIBUTE_NAME,
    BINDING_ELEMENT_NAME,
    ELSA_NAMESPACE_PREFIX,
    ELSA_NAMESPACE_URI,
    INPUT_ELEMENT_NAME,
    INPUT_NAME_ATTRIBUTE_NAME
} from '../elsa-moddle.js';

const repositoryRoot = new URL('../../../../../../../', import.meta.url);
const formats = {
    core: 'core/src/modules/Elsa.Bpmn.Interchange/Binding/BpmnActivityBindingFormat.cs',
    studio: 'studio/src/modules/Elsa.Studio.Workflows.Core/Domain/Models/Bpmn/BpmnActivityBindingFormat.cs'
};

function constantsOf(relativePath: string): Record<string, string> {
    const source = readFileSync(fileURLToPath(new URL(relativePath, repositoryRoot)), 'utf8');
    return Object.fromEntries([...source.matchAll(/public const string (\w+) = "([^"]*)";/g)].map(match => [match[1], match[2]]));
}

describe.each(Object.entries(formats))('the %s BpmnActivityBindingFormat', (_, relativePath) => {
    const constants = constantsOf(relativePath);

    it.each([
        ['NamespaceUri', ELSA_NAMESPACE_URI],
        ['BindingElementName', BINDING_ELEMENT_NAME],
        ['ActivityTypeAttributeName', ACTIVITY_TYPE_ATTRIBUTE_NAME],
        ['InputElementName', INPUT_ELEMENT_NAME],
        ['InputNameAttributeName', INPUT_NAME_ATTRIBUTE_NAME]
    ])('agrees on %s', (name, expected) => {
        expect(constants[name]).toBe(expected);
    });

    it('agrees on the prefix where it declares one', () => {
        if ('NamespacePrefix' in constants) expect(constants.NamespacePrefix).toBe(ELSA_NAMESPACE_PREFIX);
    });
});
