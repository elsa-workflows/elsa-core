/**
 * The Bpmn.Model drift guard generate-bpmn-types.js applies between Studio's pin and Core's
 * (elsa-workflows/elsa-core#8906). Every case is fed props content, or a throwaway directory tree,
 * so none of them reads or depends on the repository's own Directory.Packages.props files.
 */
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { afterEach, describe, expect, it } from 'vitest';

const require = createRequire(import.meta.url);
const { describeCoreDrift, driftSeverity, findCorePin } = require('../bpmn-model-drift.js');

const STUDIO_PROPS_PATH = '/repo/studio/src/Directory.Packages.props';
const CORE_PROPS_PATH = '/repo/Directory.Packages.props';

function studioProps(version: string, divergesFromCoreVersion?: string): string {
    const divergence = divergesFromCoreVersion
        ? `<BpmnModelDivergesFromCoreVersion>${divergesFromCoreVersion}</BpmnModelDivergesFromCoreVersion>`
        : '';
    return `<Project><PropertyGroup><BpmnModelVersion>${version}</BpmnModelVersion>${divergence}</PropertyGroup>
<ItemGroup><PackageVersion Include="Bpmn.Model" Version="$(BpmnModelVersion)" /></ItemGroup></Project>`;
}

function corePropsXml(version: string): string {
    return `<Project><ItemGroup>
<PackageVersion Include="Bpmn.Interchange" Version="${version}"/>
<PackageVersion Include="Bpmn.Model" Version="${version}"/>
</ItemGroup></Project>`;
}

function corePin(version: string) {
    return { version, path: CORE_PROPS_PATH };
}

describe('describeCoreDrift', () => {
    it('passes when Studio and Core pin the same version', () => {
        expect(describeCoreDrift(studioProps('0.3.0'), STUDIO_PROPS_PATH, corePin('0.3.0'))).toBeNull();
    });

    it('reports a mismatch, naming both pins and the override that would accept it', () => {
        const drift = describeCoreDrift(studioProps('0.3.0'), STUDIO_PROPS_PATH, corePin('0.3.1'));

        expect(drift).toContain('Studio pins Bpmn.Model 0.3.0');
        expect(drift).toContain('Core pins 0.3.1');
        expect(drift).toContain('<BpmnModelDivergesFromCoreVersion>0.3.1</BpmnModelDivergesFromCoreVersion>');
    });

    it('passes a declared divergence that names Core\'s current version', () => {
        expect(describeCoreDrift(studioProps('0.3.0', '0.3.1'), STUDIO_PROPS_PATH, corePin('0.3.1'))).toBeNull();
    });

    it('reports a divergence declared against a Core version Core has since moved on from', () => {
        const drift = describeCoreDrift(studioProps('0.3.0', '0.3.1'), STUDIO_PROPS_PATH, corePin('0.3.2'));

        expect(drift).toContain('Core pins 0.3.2');
    });

    it('reports a divergence still declared after the pins converge', () => {
        const drift = describeCoreDrift(studioProps('0.3.0', '0.3.0'), STUDIO_PROPS_PATH, corePin('0.3.0'));

        expect(drift).toContain('Remove the stale override');
    });

    it('reports a missing Core pin', () => {
        const drift = describeCoreDrift(studioProps('0.3.0'), STUDIO_PROPS_PATH, null);

        expect(drift).toContain('Could not find Core\'s Bpmn.Model pin');
    });
});

describe('driftSeverity', () => {
    it('fails --check on drift', () => {
        expect(driftSeverity('drifted', true)).toBe('error');
    });

    it('only warns about drift during plain generation', () => {
        expect(driftSeverity('drifted', false)).toBe('warning');
    });

    it('says nothing without drift', () => {
        expect(driftSeverity(null, true)).toBeNull();
        expect(driftSeverity(null, false)).toBeNull();
    });
});

describe('findCorePin', () => {
    let root: string | undefined;

    afterEach(() => {
        if (root) {
            rmSync(root, { recursive: true, force: true });
            root = undefined;
        }
    });

    function studioPropsIn(repositoryRoot: string): string {
        const studioSource = join(repositoryRoot, 'studio', 'src');
        mkdirSync(studioSource, { recursive: true });
        const studioPropsPath = join(studioSource, 'Directory.Packages.props');
        writeFileSync(studioPropsPath, studioProps('0.3.0'));
        return studioPropsPath;
    }

    it('reads the literal Bpmn.Model version from the props file above Studio\'s', () => {
        root = mkdtempSync(join(tmpdir(), 'bpmn-model-drift-'));
        const studioPropsPath = studioPropsIn(root);
        writeFileSync(join(root, 'Directory.Packages.props'), corePropsXml('0.3.1'));

        expect(findCorePin(studioPropsPath)).toEqual({ version: '0.3.1', path: join(root, 'Directory.Packages.props') });
    });

    it('fails the check when no Core props file exists above Studio\'s', () => {
        root = mkdtempSync(join(tmpdir(), 'bpmn-model-drift-'));
        const studioPropsPath = studioPropsIn(root);
        const core = findCorePin(studioPropsPath);

        expect(core).toBeNull();
        expect(driftSeverity(describeCoreDrift(studioProps('0.3.0'), studioPropsPath, core), true)).toBe('error');
    });
});
