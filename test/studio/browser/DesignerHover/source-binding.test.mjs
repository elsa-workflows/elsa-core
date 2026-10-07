import assert from 'node:assert/strict';
import {execFileSync} from 'node:child_process';
import {mkdtemp, mkdir, rm, writeFile} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {test} from 'node:test';
import {assertSourceHead, captureSourceBinding, verifySourceBinding} from './source-binding.mjs';

async function fixture(t) {
    const root = await mkdtemp(join(tmpdir(), 'designer-hover-binding-'));
    t.after(() => rm(root, {recursive: true, force: true}));
    const git = args => execFileSync('git', args, {cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore']}).trim();
    git(['init', '--quiet']);
    await writeFile(join(root, 'source.ts'), 'export const value = 1;\n');
    git(['add', 'source.ts']);
    git(['-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '--quiet', '-m', 'fixture']);
    await mkdir(join(root, 'node_modules'));
    await writeFile(join(root, 'node_modules/runtime.js'), 'runtime');
    await writeFile(join(root, 'restored.lock'), 'lock');
    const head = git(['rev-parse', 'HEAD']);
    const paths = ['source.ts', 'node_modules/runtime.js', 'restored.lock'];
    const generated = ['restored.lock'];
    return {root, head, paths, generated, git};
}

test('unchanged committed source and restored inputs pass both guards', async t => {
    const f = await fixture(t);
    const binding = await captureSourceBinding(f.root, f.paths, f.head, f.generated);
    await verifySourceBinding(f.root, binding, f.generated);
    assert.equal(binding.commit, f.head);
    assert.equal(binding.inputs.length, 3);
});

test('wrong expected head fails before capture', async t => {
    const f = await fixture(t);
    assert.throws(() => assertSourceHead(f.root, '0'.repeat(40)), /source_head_mismatch/);
    assert.throws(() => assertSourceHead(f.root, undefined), /source_head_mismatch/);
});

test('tracked source edits fail before capture', async t => {
    const f = await fixture(t);
    await writeFile(join(f.root, 'source.ts'), 'changed');
    await assert.rejects(captureSourceBinding(f.root, f.paths, f.head, f.generated));
});

test('tracked source edits after capture fail final validation', async t => {
    const f = await fixture(t);
    const binding = await captureSourceBinding(f.root, f.paths, f.head, f.generated);
    await writeFile(join(f.root, 'source.ts'), 'changed');
    await assert.rejects(verifySourceBinding(f.root, binding, f.generated));
});

for (const path of ['node_modules/runtime.js', 'restored.lock']) {
    test(`changed ${path} fails final hash validation`, async t => {
        const f = await fixture(t);
        const binding = await captureSourceBinding(f.root, f.paths, f.head, f.generated);
        await writeFile(join(f.root, path), 'changed');
        await assert.rejects(verifySourceBinding(f.root, binding, f.generated), /source_input_changed/);
    });
}

test('changed HEAD fails final validation even when input bytes match', async t => {
    const f = await fixture(t);
    const binding = await captureSourceBinding(f.root, f.paths, f.head, f.generated);
    f.git(['-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '--allow-empty', '--quiet', '-m', 'new head']);
    await assert.rejects(verifySourceBinding(f.root, binding, f.generated), /source_head_mismatch/);
});

test('untracked source cannot be attributed to HEAD', async t => {
    const f = await fixture(t);
    await writeFile(join(f.root, 'untracked.ts'), 'untracked');
    await assert.rejects(captureSourceBinding(f.root, [...f.paths, 'untracked.ts'], f.head, f.generated));
});
