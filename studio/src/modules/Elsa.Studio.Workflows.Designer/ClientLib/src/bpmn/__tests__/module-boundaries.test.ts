/**
 * The scope guard for src/bpmn.
 *
 * The whole point of this module is that it is canvas-neutral: an X6 adapter and a later React Flow
 * adapter both consume it, so it may not know about either. A single stray `import` from `@antv/x6`
 * or `../react-designer` would compile, pass every other test, and quietly make the module
 * un-reusable -- which is exactly the kind of regression that only a check like this catches.
 */
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const MODULE_DIRECTORY = join(dirname(fileURLToPath(import.meta.url)), '..');

/** Packages this module may never reach for, whatever the import syntax. */
const FORBIDDEN_PACKAGES = [
    '@antv/x6',
    '@antv/layout',
    '@xyflow/react',
    'react',
    'react-dom',
];

/** Sibling source trees this module may never reach into. */
const FORBIDDEN_SIBLING_DIRECTORIES = ['designer', 'react-designer'];

describe('src/bpmn module boundaries', () => {
    const files = collectTypeScriptFiles(MODULE_DIRECTORY).map(file => relative(MODULE_DIRECTORY, file));

    it('has source files to check', () => {
        // Guards the guard: a path bug that found nothing would otherwise report success.
        expect(files.length).toBeGreaterThan(5);
    });

    it.each(files)('%s imports neither canvas package nor designer source', file => {
        const offending = readImportSpecifiers(readFileSync(join(MODULE_DIRECTORY, file), 'utf8')).filter(isForbidden);

        expect(offending, `${file} must stay canvas-neutral`).toEqual([]);
    });
});

function isForbidden(specifier: string): boolean {
    if (FORBIDDEN_PACKAGES.some(name => specifier === name || specifier.startsWith(`${name}/`))) return true;

    // Anything that climbs out of src/bpmn and back down into a sibling designer tree, at any depth.
    const segments = specifier.split('/');

    return segments[0] === '..'
        && segments.some(segment => FORBIDDEN_SIBLING_DIRECTORIES.includes(segment));
}

/** Static imports and re-exports, `import type`, dynamic `import()` and CommonJS `require()`. */
function readImportSpecifiers(source: string): string[] {
    const patterns = [
        /\b(?:import|export)\s[^;'"]*?\bfrom\s*['"]([^'"]+)['"]/g,
        /\bimport\s*['"]([^'"]+)['"]/g,
        /\bimport\s*\(\s*['"]([^'"]+)['"]\s*\)/g,
        /\brequire\s*\(\s*['"]([^'"]+)['"]\s*\)/g,
    ];

    return patterns.flatMap(pattern => Array.from(source.matchAll(pattern), match => match[1]));
}

function collectTypeScriptFiles(directory: string): string[] {
    return readdirSync(directory).flatMap(entry => {
        const path = join(directory, entry);

        if (statSync(path).isDirectory()) return collectTypeScriptFiles(path);

        return path.endsWith('.ts') || path.endsWith('.tsx') ? [path] : [];
    });
}
