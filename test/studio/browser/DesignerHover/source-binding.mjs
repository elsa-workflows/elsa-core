import {execFileSync} from 'node:child_process';
import {createHash} from 'node:crypto';
import {readFile} from 'node:fs/promises';
import {join, isAbsolute} from 'node:path';

const git = (root, args) => execFileSync('git', args, {cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore']}).trim();
const hash = bytes => createHash('sha256').update(bytes).digest('hex');

export function assertSourceHead(root, expectedHead) {
    if (!/^[0-9a-f]{40}$/.test(expectedHead) || git(root, ['rev-parse', 'HEAD']) !== expectedHead) {
        throw new Error('source_head_mismatch');
    }
}

function assertTrackedInputs(root, paths, generatedPaths) {
    for (const path of paths) {
        if (!path || isAbsolute(path) || path.split(/[\\/]/).includes('..')) {
            throw new Error('source_input_path');
        }
    }
    const tracked = paths.filter(path => !path.split('/').includes('node_modules') && !generatedPaths.includes(path));
    git(root, ['ls-files', '--error-unmatch', '--', ...tracked]);
    git(root, ['diff', '--quiet', 'HEAD', '--', ...tracked]);
}

export async function captureSourceBinding(root, paths, expectedHead, generatedPaths = []) {
    assertSourceHead(root, expectedHead);
    const sorted = [...new Set(paths)].sort();
    assertTrackedInputs(root, sorted, generatedPaths);
    const inputs = [];
    for (const path of sorted) {
        const bytes = await readFile(join(root, path));
        inputs.push({path, sha256: hash(bytes), bytes: bytes.length});
    }
    return {commit: expectedHead, inputs};
}

export async function verifySourceBinding(root, binding, generatedPaths = []) {
    assertSourceHead(root, binding.commit);
    assertTrackedInputs(root, binding.inputs.map(input => input.path), generatedPaths);
    for (const input of binding.inputs) {
        const bytes = await readFile(join(root, input.path));
        if (hash(bytes) !== input.sha256 || bytes.length !== input.bytes) {
            throw new Error('source_input_changed');
        }
    }
}
