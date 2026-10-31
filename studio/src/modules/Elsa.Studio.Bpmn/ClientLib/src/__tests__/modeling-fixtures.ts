import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

// Paths, not URLs: jsdom replaces the global URL with one Node's fileURLToPath does not accept.
const repositoryRoot = join(fileURLToPath(import.meta.url), '../../../../../../../../');

/** The designer contract suite's fixtures (studio/test/browser/BpmnDesigner/fixtures). */
export const FIXTURES = join(repositoryRoot, 'studio/test/browser/BpmnDesigner/fixtures');

/** Elsa's BPMN import test assets, which every designer must round-trip. */
export const CORE_ASSETS = join(repositoryRoot, 'core/test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets');

/** The contract suite fixture `file`. */
export function fixture(file: string): string {
    return readFileSync(join(FIXTURES, file), 'utf8');
}

/** Each `.bpmn` file in `directory`, by name, with its content. */
export function bpmnFiles(directory: string): [string, string][] {
    return readdirSync(directory)
        .filter(file => file.endsWith('.bpmn'))
        .sort()
        .map(file => [file, readFileSync(join(directory, file), 'utf8')]);
}
