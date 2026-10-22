import assert from 'node:assert/strict';
import { createDesignerStore } from '../packages/sdk/dist/designer/store.js';
import { createElsaClient } from '../packages/api-client/dist/index.js';
const { ELSA_TEST_API_URL: baseUrl, ELSA_TEST_USERNAME: username, ELSA_TEST_PASSWORD: password } = process.env;
if (!baseUrl || !username || !password) throw new Error('Set ELSA_TEST_API_URL, ELSA_TEST_USERNAME and ELSA_TEST_PASSWORD for a disposable development server.');
const response = await fetch(`${baseUrl}/identity/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ username, password }) });
if (!response.ok) throw new Error(`Development server login failed: ${response.status}`);
const login = await response.json();
assert(login.isAuthenticated && login.accessToken, 'Development server authentication failed');
const client = createElsaClient({ baseUrl, getAuthHeaders: () => ({ Authorization: `Bearer ${login.accessToken}` }) });
const cleanup = [];
let primaryError;
try {
  const name = `SettingsRoundtrip${Date.now()}`;
  const model = { name, variables: [], inputs: [], outputs: [], outcomes: [], customProperties: {}, options: { usableAsActivity: true, autoUpdateConsumingWorkflows: true }, root: { id: 'child-root', type: 'Elsa.Sequence', version: 1, activities: [] } };
  const first = (await client.workflowDefinitions.save({ model, publish: true })).workflowDefinition;
  cleanup.push(first.definitionId);
  const second = (await client.workflowDefinitions.save({ model: { ...first, description: 'Second version' }, publish: true })).workflowDefinition;
  assert.equal(second.version, 2);
  const descriptors = await client.descriptors.activities(true);
  const descriptor = descriptors.items.find(item => item.customProperties?.WorkflowDefinitionId === first.definitionId && item.version === 2);
  assert(descriptor, 'Reusable workflow descriptor version 2 missing');
  const commitStrategies = await client.descriptors.activityCommitStrategies();
  assert(commitStrategies.items.length > 0, 'Commit strategy catalog is empty');
  const logStrategies = await client.descriptors.logPersistenceStrategies();
  assert(logStrategies.items.length > 0, 'Log persistence catalog is empty');
  const customProperties = { commitStrategyName: commitStrategies.items[0].name, runAsynchronously: true,
    logPersistenceConfig: { default: { evaluationMode: 'Strategy', strategyType: logStrategies.items[0].typeName }, internalState: { evaluationMode: 'Expression', expression: { type: 'Literal', value: 'Exclude' } }, inputs: { message: { evaluationMode: 'Strategy', strategyType: 'Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core' } }, outputs: { result: null } },
    resilienceStrategy: { mode: 'Expression', expression: { type: 'JavaScript', value: '"Default"' } }, pluginData: { keep: true } };
  const store = createDesignerStore({ id: 'parent-root', type: 'Elsa.Flowchart', version: 1, activities: [], connections: [] });
  const child = store.getState().addActivity(descriptor, { x: 0, y: 0 }, { customProperties });
  assert.equal(child.workflowDefinitionId, first.definitionId);
  assert.equal(child.workflowDefinitionVersionId, second.id);
  const parent = (await client.workflowDefinitions.save({ model: { ...model, name: `${name}Parent`, options: {}, root: { id: 'parent-root', type: 'Elsa.Flowchart', version: 1, activities: [child], connections: [] } } })).workflowDefinition;
  cleanup.push(parent.definitionId);
  const loaded = await client.workflowDefinitions.getByDefinitionId(parent.definitionId);
  const saved = loaded.root.activities.find(item => item.id === child.id);
  for (const [key, value] of Object.entries(customProperties)) assert.deepEqual(saved.customProperties[key], value);
  assert.equal(saved.version, 2);
  assert.equal(saved.workflowDefinitionVersionId, second.id);
  const versions = await client.workflowDefinitions.list({ definitionIds: [first.definitionId], versionOptions: 'AllVersions', page: 0, pageSize: 10 });
  assert(versions.items.some(item => item.id === second.id && item.version === 2));
  const published = await client.workflowDefinitions.publish(parent.definitionId);
  assert.equal(published.workflowDefinition.isPublished, true);
  const retracted = await client.workflowDefinitions.retract(parent.definitionId);
  assert.equal(retracted.definitionId, parent.definitionId);
  assert.equal(retracted.isPublished, false);
  assert.equal((await client.workflowDefinitions.getByDefinitionId(parent.definitionId)).isPublished, false);
  const reverted = await client.workflowDefinitions.revert(first.definitionId, 1);
  assert.equal(reverted.definitionId, first.definitionId);
  assert.equal(reverted.version, 3);
  assert.equal((await client.workflowDefinitions.getByDefinitionId(first.definitionId)).id, reverted.id);
  const updated = await client.workflowDefinitions.updateReferences(first.definitionId);
  assert(updated.affectedWorkflows.includes(parent.name));
  console.log('PASS: actual Core API round-trips commit, logging strategy/expression, resilience expression, task mode, custom data, and the reusable activity version pair; dedicated publish, retract, revert and update-references actions accept their requests and return the expected models');
} catch (error) {
  primaryError = error;
  throw error;
} finally {
  const results = await Promise.allSettled(cleanup.reverse().map(id => client.workflowDefinitions.delete(id)));
  const failures = results.filter(result => result.status === 'rejected');
  if (failures.length) throw new AggregateError([...(primaryError ? [primaryError] : []), ...failures.map(result => result.reason)],
    primaryError ? 'Settings check and temporary definition cleanup failed' : 'Temporary definition cleanup failed', { cause: primaryError });
}
