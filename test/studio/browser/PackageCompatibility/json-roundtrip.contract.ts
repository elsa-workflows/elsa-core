import assert from 'node:assert/strict';
import { maxWorkflowJsonBytes, readWorkflowJsonExport, workflowDefinitionSchema, workflowSemanticIdentity } from './json-roundtrip.js';

function definition() {
  const inputs: Array<{ name: string }> = [];
  return {
    $schema: workflowDefinitionSchema,
    id: 'candidate-version-id',
    definitionId: 'candidate-definition-id',
    tenantId: 'candidate-tenant',
    name: 'candidate-workflow',
    description: '',
    createdAt: '2026-10-07T00:00:00Z',
    version: 1,
    toolVersion: '3.10.0.0',
    variables: [],
    inputs,
    outputs: [{ type: 'String', name: 'sentinel', displayName: 'sentinel', description: '', category: 'Primitives' }],
    outcomes: [],
    customProperties: { source: 'native-editor' },
    isReadonly: false,
    isSystem: false,
    isLatest: true,
    isPublished: false,
    options: { usableAsActivity: false },
    root: {
      type: 'Elsa.Flowchart', id: 'root-id', nodeId: 'Workflow1:root-id', name: 'Flowchart1', version: 1,
      customProperties: {}, variables: [], connections: [],
      activities: [{
        type: 'Elsa.SetOutput', id: 'activity-id', nodeId: 'Workflow1:root-id:activity-id', name: 'SetOutput1', version: 1,
        customProperties: {},
        outputName: { typeName: 'String', expression: { type: 'Literal', value: 'sentinel' } },
        outputValue: { typeName: 'Object', expression: { type: 'Literal', value: 'synthetic-value' } }
      }]
    }
  };
}

const source = definition();
const raw = Buffer.from(JSON.stringify(source));
const checked = readWorkflowJsonExport(raw);
assert.equal(checked.bytes, raw.length);
assert.equal(checked.documentSha256.length, 64);
assert.equal(checked.definitionId, source.definitionId);
assert.equal(checked.rootId, source.root.id);
assert.deepEqual(checked.activityIds, ['activity-id']);
assert.equal(checked.outputName, 'sentinel');
assert.equal(checked.outputValue, 'synthetic-value');
assert.equal(checked.semanticSha256, workflowSemanticIdentity(source).semanticSha256);

const apiReadback = structuredClone(source);
delete (apiReadback as Partial<typeof source>).$schema;
apiReadback.id = 'new-nonsemantic-version-id';
apiReadback.version = 2;
apiReadback.createdAt = '2026-10-08T00:00:00Z';
apiReadback.isLatest = false;
apiReadback.isPublished = true;
Object.assign(apiReadback, { links: [{ href: '/workflow-definitions/by-definition-id/candidate-definition-id', rel: 'self', method: 'GET' }] });
assert.equal(workflowSemanticIdentity(apiReadback).semanticSha256, checked.semanticSha256,
  'API navigation links and known export/version metadata must not change semantic identity');

const semanticMutations: Array<(document: ReturnType<typeof definition>) => void> = [
  document => { document.definitionId = 'changed-definition'; },
  document => { document.root.id = 'changed-root'; },
  document => { document.root.activities[0].id = 'changed-activity'; },
  document => { document.root.activities[0].outputValue.expression.value = 'changed-value'; },
  document => {
    document.root.activities[0].outputName.expression.value = 'changed-output';
    document.outputs[0].name = 'changed-output';
  },
  document => { document.tenantId = 'different-tenant-scope'; },
  document => { document.inputs.push({ name: 'changed-input' }); },
  document => { document.options.usableAsActivity = true; },
  document => { document.customProperties.source = 'changed-source'; },
  document => { Object.assign(document.root.customProperties, { links: ['semantic-nested-value'] }); },
  document => { (document as any).futureSemanticField = 'preserved-by-default'; }
];
for (const mutate of semanticMutations) {
  const changed = structuredClone(source);
  mutate(changed);
  assert.notEqual(workflowSemanticIdentity(changed).semanticSha256, checked.semanticSha256);
}

const nonLiteralExpression = structuredClone(source);
nonLiteralExpression.root.activities[0].outputValue.expression.type = 'JavaScript';
assert.throws(() => workflowSemanticIdentity(nonLiteralExpression), /workflow_output_value_literal_missing/);

assert.equal(workflowSemanticIdentity({ ...source, root: { ...source.root, customProperties: { a: 1, b: 2 } } }).semanticSha256,
  workflowSemanticIdentity({ ...source, root: { ...source.root, customProperties: { b: 2, a: 1 } } }).semanticSha256,
  'Object key order must not change semantic identity');

assert.throws(() => readWorkflowJsonExport(Buffer.alloc(0)));
assert.throws(() => readWorkflowJsonExport(Buffer.alloc(maxWorkflowJsonBytes + 1)));
assert.throws(() => readWorkflowJsonExport(Buffer.from([0xc3, 0x28])));
assert.throws(() => readWorkflowJsonExport(Buffer.from('{')));
assert.throws(() => readWorkflowJsonExport(Buffer.from(JSON.stringify({ ...source, $schema: 'https://invalid/schema.json' }))));
assert.throws(() => readWorkflowJsonExport(Buffer.from(JSON.stringify({ ...source, $schema: undefined }))));

const unboundOutput = structuredClone(source);
unboundOutput.outputs = [];
assert.throws(() => workflowSemanticIdentity(unboundOutput), /workflow_candidate_output_unbound/);

const ambiguousCandidate = structuredClone(source);
ambiguousCandidate.root.activities.push(structuredClone(ambiguousCandidate.root.activities[0]));
assert.throws(() => workflowSemanticIdentity(ambiguousCandidate), /workflow_candidate_activity_count_invalid/);

process.stdout.write('JSON roundtrip contracts passed\n');
