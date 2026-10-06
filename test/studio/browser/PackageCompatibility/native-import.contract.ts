// Generated contract input from the Python fixture; no package/runtime proof.
import assert from 'node:assert/strict';
import { chmodSync, readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { checkReleasedDefinition, readReleasedInput, resourceBody } from './journey.js';

const response = (body: Buffer, length?: string) => ({ headers: (): Record<string, string> => length === undefined ? {} : { 'content-length': length }, body: async () => body });
const bytes = Buffer.from('observed decoded package bytes');
for (const length of [undefined, String(bytes.length), '12'])
  assert.deepEqual(await resourceBody(response(bytes, length), bytes.length), bytes);
for (const length of ['', '-1', '1.5', 'NaN', String(bytes.length + 1), '9007199254740992'])
  await assert.rejects(resourceBody(response(bytes, length), bytes.length));
await assert.rejects(resourceBody(response(Buffer.from('different bytes')), bytes.length));
await assert.rejects(resourceBody(response(bytes), 32 * 1024 * 1024 + 1));

const inputs = JSON.parse(readFileSync(0, 'utf8')) as Array<Parameters<typeof readReleasedInput>[0]>;
for (const input of inputs) {
  const { raw, document } = readReleasedInput(input);
  checkReleasedDefinition(structuredClone(document), document);
  const reversed = { ...document, outputs: [Object.fromEntries(Object.entries(document.outputs[0]).reverse())] };
  checkReleasedDefinition(reversed, document); // JSON key order is not a semantic change.
  const mutations: Array<(d: any) => void> = [
    d => { d.definitionId = 'changed'; }, d => { d.root.id = 'changed'; },
    d => { d.root.activities.push(structuredClone(d.root.activities[0])); },
    d => { d.root.activities[0].outputValue.expression.type = 'JavaScript'; },
    d => { d.root.activities[0].outputValue.expression.value = 'changed'; },
    d => { d.root.activities[0].id = 'changed'; }, d => { d.outputs[0].name = 'changed'; },
    d => { d.inputs.push({ name: 'private' }); }, d => { d.toolVersion = '3.10.0.0'; },
  ];
  for (const mutate of mutations) {
    const changed = structuredClone(document); mutate(changed);
    assert.throws(() => checkReleasedDefinition(changed, document));
  }
  const changedInput = structuredClone(input);
  changedInput.binding.document.document_sha256 = '0'.repeat(64);
  assert.throws(() => readReleasedInput(changedInput));
  try {
    chmodSync(input.private_path, 0o644);
    assert.throws(() => readReleasedInput(input));
    chmodSync(input.private_path, 0o600);
    writeFileSync(input.private_path, Buffer.alloc(1024 * 1024 + 1));
    assert.throws(() => readReleasedInput(input));
    const invalid = structuredClone(document);
    invalid.root.activities[0].outputValue.expression.type = 'JavaScript';
    const bad = Buffer.from(JSON.stringify(invalid));
    writeFileSync(input.private_path, bad);
    const forged = structuredClone(input);
    forged.binding.document.bytes = bad.length;
    forged.binding.document.document_sha256 = createHash('sha256').update(bad).digest('hex');
    assert.throws(() => readReleasedInput(forged)); // Rehashing caller data cannot bypass graph guards.
  } finally {
    writeFileSync(input.private_path, raw); chmodSync(input.private_path, 0o600);
  }
}
process.stdout.write('native import contracts passed\n');
