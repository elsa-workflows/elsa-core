/**
 * The `elsa:` BPMN vendor extension, as a moddle package descriptor.
 *
 * The names are the ones `BpmnActivityBindingFormat` defines in Elsa.Bpmn.Interchange (and mirrors in
 * Elsa.Studio.Workflows.Core). They are a compatibility surface: a document exported with other names reads back as a
 * task nobody bound. __tests__/format-drift.test.ts fails when these and the C# constants disagree.
 */

export const ELSA_NAMESPACE_URI = 'https://elsaworkflows.io/schemas/bpmn/v1';
export const ELSA_NAMESPACE_PREFIX = 'elsa';
export const BINDING_ELEMENT_NAME = 'activityBinding';
export const ACTIVITY_TYPE_ATTRIBUTE_NAME = 'activityType';
export const INPUT_ELEMENT_NAME = 'input';
export const INPUT_NAME_ATTRIBUTE_NAME = 'name';

/** The moddle type of an `elsa:activityBinding` element. */
export const BINDING_TYPE = `${ELSA_NAMESPACE_PREFIX}:ActivityBinding`;

/** The moddle type of an `elsa:input` element. */
export const INPUT_TYPE = `${ELSA_NAMESPACE_PREFIX}:Input`;

/**
 * Pass as `moddleExtensions: { elsa: elsaModdleDescriptor }` (bpmn-js) or to `new BpmnModdle({ elsa: ... })`.
 * `tagAlias: lowerCase` maps the types `ActivityBinding` and `Input` to the tags `activityBinding` and `input`.
 */
export const elsaModdleDescriptor = {
    name: 'Elsa',
    uri: ELSA_NAMESPACE_URI,
    prefix: ELSA_NAMESPACE_PREFIX,
    xml: { tagAlias: 'lowerCase' },
    associations: [],
    types: [
        {
            name: 'ActivityBinding',
            superClass: ['Element'],
            properties: [
                { name: ACTIVITY_TYPE_ATTRIBUTE_NAME, type: 'String', isAttr: true },
                { name: 'inputs', type: 'Input', isMany: true }
            ]
        },
        {
            name: 'Input',
            superClass: ['Element'],
            properties: [
                { name: INPUT_NAME_ATTRIBUTE_NAME, type: 'String', isAttr: true },
                { name: 'value', type: 'String', isBody: true }
            ]
        }
    ],
    enumerations: []
} as const;
