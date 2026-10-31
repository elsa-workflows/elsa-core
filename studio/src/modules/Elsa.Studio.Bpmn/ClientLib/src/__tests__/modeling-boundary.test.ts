// The modeling kernel is canvas-neutral: a designer on any canvas builds on it, so it may not depend on one.
import { readdirSync, readFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const modeling = join(fileURLToPath(import.meta.url), '../../modeling');
const FORBIDDEN = /^(@antv\/|react(-dom)?(\/|$)|bpmn-js(\/|$)|diagram-js(\/|$))/;

function importsOf(source: string): string[] {
    const specifiers = [...source.matchAll(/\b(?:import|export)\b[^'"]*?\bfrom\s*['"]([^'"]+)['"]|\bimport\s*\(?\s*['"]([^'"]+)['"]/g)];
    return specifiers.map(match => match[1] ?? match[2]);
}

/** Every package `file` imports, directly or through the sources it imports. */
function packagesImportedBy(file: string, seen = new Set<string>()): string[] {
    if (seen.has(file)) return [];
    seen.add(file);
    return importsOf(readFileSync(file, 'utf8')).flatMap(specifier =>
        specifier.startsWith('.') ? packagesImportedBy(resolve(dirname(file), specifier.replace(/\.js$/, '.ts')), seen) : [specifier]);
}

describe('the modeling kernel', () => {
    const files = readdirSync(modeling).filter(file => file.endsWith('.ts'));

    it('has sources to check', () => {
        expect(files).toContain('index.ts');
    });

    it.each(files)('%s imports nothing from X6, React, bpmn-js or diagram-js, directly or through what it imports', file => {
        expect(packagesImportedBy(join(modeling, file)).filter(specifier => FORBIDDEN.test(specifier))).toEqual([]);
    });

    it('recognises the imports it forbids', () => {
        const source = `import { Graph } from '@antv/x6';\nimport React from 'react';\nimport Modeler from 'bpmn-js/lib/Modeler';\nexport { x } from 'diagram-js';\nimport('react-dom/client');`;

        expect(importsOf(source).filter(specifier => FORBIDDEN.test(specifier))).toEqual(['@antv/x6', 'react', 'bpmn-js/lib/Modeler', 'diagram-js', 'react-dom/client']);
    });

    it('follows the sources it imports', () => {
        expect(packagesImportedBy(join(modeling, 'index.ts'))).toEqual(expect.arrayContaining(['bpmn-moddle', 'bpmn-auto-layout']));
    });
});
