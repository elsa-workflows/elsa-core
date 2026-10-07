import assert from 'node:assert/strict';
import { checkReactDefinition, reactAfterValue, reactChecks, reactHash, type ReactHashes } from './react-phase.js';

const document = {
  definitionId: 'abc', name: 'paired-browser-012345abcdef', outputs: [{ name: 'sentinel' }],
  root: { id: 'def', type: 'Elsa.Flowchart', nodeId: 'Workflow1:def', activities: [
    { id: '123', type: 'Elsa.SetOutput', nodeId: 'Workflow1:def:123',
      outputName: { typeName: 'String', expression: { type: 'Literal', value: 'sentinel' } },
      outputValue: { typeName: 'Object', expression: { type: 'Literal', value: 'synthetic-browser-value' } } }
  ] }
};
const expected: ReactHashes = { definition_id_sha256: reactHash('abc'), root_id_sha256: reactHash('def'), activity_id_sha256: reactHash('123'), before_value_sha256: reactHash('synthetic-browser-value') };
assert.equal(reactChecks.length, new Set(reactChecks).size);
assert.equal(checkReactDefinition(document, expected, false).activityId, '123');
assert.throws(() => checkReactDefinition(document, expected, true));
const changed = structuredClone(document);
changed.root.activities[0].outputValue.expression.value = reactAfterValue;
assert.equal(checkReactDefinition(changed, expected, true).value, reactAfterValue);
assert.throws(() => checkReactDefinition(changed, expected, false));
for (const mutate of [
  (value: typeof document) => { value.definitionId = '999'; },
  (value: typeof document) => { value.root.id = '999'; },
  (value: typeof document) => { value.root.activities[0].id = '999'; },
  (value: typeof document) => { value.root.type = 'Elsa.BpmnProcess'; },
  (value: typeof document) => { value.root.activities[0].outputValue.expression.type = 'JavaScript'; },
  (value: typeof document) => { value.root.activities[0].outputValue.expression.value = 'wrong'; },
  (value: typeof document) => { value.root.activities[0].outputValue.typeName = 'String'; },
  (value: typeof document) => { value.root.activities[0].outputName.expression.value = 'wrong'; },
  (value: typeof document) => { value.root.activities.push(structuredClone(value.root.activities[0])); },
  (value: typeof document) => { value.root.activities[0].id = 'unsafe"selector'; }
]) {
  const wrong = structuredClone(changed); mutate(wrong);
  assert.throws(() => checkReactDefinition(wrong, expected, true));
}
for (const name of Object.keys(expected) as (keyof ReactHashes)[])
  assert.throws(() => checkReactDefinition(document, { ...expected, [name]: reactHash('wrong') }, false));
assert.throws(() => checkReactDefinition(changed, { ...expected, before_value_sha256: reactHash(reactAfterValue) }, true));
console.log('React phase contracts passed');
