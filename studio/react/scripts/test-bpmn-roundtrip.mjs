import assert from 'node:assert/strict';
import { createElsaClient, ApiError } from '../packages/api-client/dist/index.js';
const { ELSA_TEST_API_URL: baseUrl, ELSA_TEST_USERNAME: username, ELSA_TEST_PASSWORD: password } = process.env;
if (!baseUrl || !username || !password) throw new Error('Set ELSA_TEST_API_URL, ELSA_TEST_USERNAME and ELSA_TEST_PASSWORD for a disposable development server.');
const response = await fetch(`${baseUrl}/identity/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ username, password }) });
if (!response.ok) throw new Error(`Development server login failed: ${response.status}`);
const login = await response.json();
assert(login.isAuthenticated && login.accessToken, 'Development server authentication failed');
const client = createElsaClient({ baseUrl, getAuthHeaders: () => ({ Authorization: `Bearer ${login.accessToken}` }) });
const xml = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" id="Definitions_preview" targetNamespace="https://elsaworkflows.io/tests">
  <bpmn:process id="preview-roundtrip" name="Preview roundtrip" isExecutable="true">
    <bpmn:startEvent id="Start"><bpmn:outgoing>Flow</bpmn:outgoing></bpmn:startEvent>
    <bpmn:endEvent id="End"><bpmn:incoming>Flow</bpmn:incoming></bpmn:endEvent>
    <bpmn:sequenceFlow id="Flow" sourceRef="Start" targetRef="End"/>
  </bpmn:process>
</bpmn:definitions>`;
const cleanup = [];
let primaryError;
try {
  const file = new File([xml], 'preview.bpmn', { type: 'application/xml' });
  const analysis = await client.bpmn.analyze(file);
  assert.deepEqual(analysis.processIds, ['preview-roundtrip']);
  const first = await client.bpmn.import(file, { name: `BpmnRoundtrip${Date.now()}` });
  cleanup.push(first.definitionId);
  const exported = await (await client.bpmn.export(first.definitionId)).text();
  const exportedAnalysis = await client.bpmn.analyze(new File([exported], 'exported.bpmn'));
  assert.deepEqual(exportedAnalysis.processIds, analysis.processIds);
  for (const element of ['startEvent', 'endEvent', 'sequenceFlow']) assert.equal(exportedAnalysis.elementCounts[element], analysis.elementCounts[element]);
  const second = await client.bpmn.import(new File([exported], 'exported.bpmn'), { name: `BpmnReimport${Date.now()}` });
  cleanup.push(second.definitionId);
  assert.equal(await (await client.bpmn.export(second.definitionId)).text(), exported);
  const condition = `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" id="Definitions_condition" targetNamespace="https://elsaworkflows.io/tests">
  <bpmn:process id="preview-condition" isExecutable="true">
    <bpmn:startEvent id="Start"><bpmn:outgoing>Entry</bpmn:outgoing></bpmn:startEvent>
    <bpmn:exclusiveGateway id="Choose" default="Flow_Default">
      <bpmn:incoming>Entry</bpmn:incoming>
      <bpmn:outgoing>Flow</bpmn:outgoing>
      <bpmn:outgoing>Flow_Default</bpmn:outgoing>
    </bpmn:exclusiveGateway>
    <bpmn:endEvent id="TrueEnd"><bpmn:incoming>Flow</bpmn:incoming></bpmn:endEvent>
    <bpmn:endEvent id="DefaultEnd"><bpmn:incoming>Flow_Default</bpmn:incoming></bpmn:endEvent>
    <bpmn:sequenceFlow id="Entry" sourceRef="Start" targetRef="Choose"/>
    <bpmn:sequenceFlow id="Flow" sourceRef="Choose" targetRef="TrueEnd">
      <bpmn:conditionExpression language="JavaScript">true</bpmn:conditionExpression>
    </bpmn:sequenceFlow>
    <bpmn:sequenceFlow id="Flow_Default" sourceRef="Choose" targetRef="DefaultEnd"/>
  </bpmn:process>
</bpmn:definitions>`;
  const conditional = await client.bpmn.import(new File([condition], 'conditional.bpmn'), { name: `BpmnConditional${Date.now()}` });
  cleanup.push(conditional.definitionId);
  const conditionalExport = await (await client.bpmn.export(conditional.definitionId)).text();
  assert.match(conditionalExport, /conditionExpression[^>]*language="JavaScript"[^>]*>true</);
  const unsupported = condition.replace('language="JavaScript"', 'language="XPath"');
  await assert.rejects((async () => {
    const unexpected = await client.bpmn.import(new File([unsupported], 'unsupported-condition.bpmn'));
    cleanup.push(unexpected.definitionId);
  })(), error => {
    assert(error instanceof ApiError);
    assert.equal(error.problem.raw.code, 'bpmn.import.condition-language-unsupported');
    assert.deepEqual(error.problem.raw.data.elementIds, ['Flow']);
    return true;
  });
  console.log('PASS: actual Core API analyzes, imports, exports canonical XML without changing process/flow counts, reimports and exports identically, preserves supported expression conditions, and refuses unsupported condition languages with structured flow IDs');
} catch (error) {
  primaryError = error;
  throw error;
} finally {
  const results = await Promise.allSettled(cleanup.reverse().map(id => client.workflowDefinitions.delete(id)));
  const failures = results.filter(result => result.status === 'rejected').map(result => result.reason);
  if (failures.length) throw new AggregateError([...(primaryError ? [primaryError] : []), ...failures], 'BPMN check cleanup failed', { cause: primaryError });
}
