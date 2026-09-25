// Generates src/bpmn/types.generated.ts from the schema Bpmn.Model publishes in its own nupkg
// (schema/bpmn-payload.schema.json). See elsa-workflows/elsa-core#7909 (W12) and studio issue #997:
// the point of generating rather than hand-mirroring is that this file, and only this file, ever
// has to change when the library's payload format version changes.
//
// Usage:
//   node scripts/generate-bpmn-types.js          Regenerates src/bpmn/types.generated.ts in place.
//                                                 Missing schema (NuGet cache not populated) is a
//                                                 warning, not a failure: a clean clone still builds
//                                                 off the checked-in file.
//   node scripts/generate-bpmn-types.js --check  Regenerates into memory and fails if that differs
//                                                 from the checked-in file, or if the schema cannot be
//                                                 found at all (this mode exists to prove it can be).
//
// The schema's version is not copied here: it is read straight out of the same
// Directory.Packages.props entry that pins the NuGet restore, so there is exactly one place that
// names the version this repository builds against.
'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFileSync } = require('child_process');
const { compile } = require('json-schema-to-typescript');

const REPO_ROOT = path.resolve(__dirname, '..', '..', '..', '..', '..');
const PACKAGES_PROPS_PATH = path.join(REPO_ROOT, 'Directory.Packages.props');
const OUTPUT_PATH = path.join(__dirname, '..', 'src', 'bpmn', 'types.generated.ts');
const PACKAGE_ID = 'Bpmn.Model';
const VERSION_PROPERTY_NAME = 'BpmnModelVersion';

// A raw `[k: string]: unknown` (or `any`) index signature is the old hand-written mirror's failure
// mode: it means some object in the schema lost its shape (usually a missing `additionalProperties:
// false`) and the generator papered over it. A *typed* dictionary the schema declares on purpose --
// e.g. `bpmnDiagnosticEvent.details`, which is `additionalProperties: { type: "string" }` -- still
// compiles to an index signature, just not one typed `unknown`/`any`, so it is not what this guards
// against.
const LOOSE_INDEX_SIGNATURE = /\[k: string\]:\s*(unknown|any)\b/;

function readPinnedVersion() {
    const xml = fs.readFileSync(PACKAGES_PROPS_PATH, 'utf8');
    const pattern = new RegExp(`<${VERSION_PROPERTY_NAME}>([^<]+)</${VERSION_PROPERTY_NAME}>`);
    const match = xml.match(pattern);

    if (!match) {
        throw new Error(`Could not find a <${VERSION_PROPERTY_NAME}>...</${VERSION_PROPERTY_NAME}> property in ${PACKAGES_PROPS_PATH}.`);
    }

    return match[1];
}

function resolveNuGetGlobalPackagesFolder() {
    if (process.env.NUGET_PACKAGES) {
        return process.env.NUGET_PACKAGES;
    }

    try {
        const output = execFileSync('dotnet', ['nuget', 'locals', 'global-packages', '--list'], { encoding: 'utf8' });
        const match = output.match(/global-packages:\s*(.+)/i);

        if (match) {
            return match[1].trim();
        }
    } catch {
        // dotnet is not on PATH, or the command failed; fall through to the conventional default.
    }

    return path.join(os.homedir(), '.nuget', 'packages');
}

function resolveSchemaPath(version) {
    const nugetFolder = resolveNuGetGlobalPackagesFolder();
    // NuGet's global-packages folder always keys on the lower-cased package id.
    return path.join(nugetFolder, PACKAGE_ID.toLowerCase(), version, 'schema', 'bpmn-payload.schema.json');
}

// json-schema-to-typescript names an enum from its own `tsEnumNames` field. The schema instead
// publishes names under `x-enumNames` (the convention its own tooling already uses elsewhere), so
// this copies one to the other wherever both are present -- a mechanical bridge between the two
// conventions, not a hand-authored fact about any particular enum.
function bridgeEnumNames(node) {
    if (!node || typeof node !== 'object') {
        return;
    }

    if (Array.isArray(node.enum) && Array.isArray(node['x-enumNames']) && node.enum.length === node['x-enumNames'].length) {
        node.tsEnumNames = node['x-enumNames'];
    }

    for (const value of Object.values(node)) {
        bridgeEnumNames(value);
    }
}

async function generate(schemaPath) {
    const schema = JSON.parse(fs.readFileSync(schemaPath, 'utf8'));
    const formatVersion = schema['x-payloadFormatVersion'];
    bridgeEnumNames(schema);

    const body = await compile(schema, 'BpmnPayload', {
        bannerComment: '',
        style: { semi: true, singleQuote: true },
        enableConstEnums: false,
        format: false,
    });

    if (LOOSE_INDEX_SIGNATURE.test(body)) {
        throw new Error(
            'json-schema-to-typescript emitted a `[k: string]: unknown`/`any` index signature. '
            + 'That is the failure mode this generator exists to close (see the schema for the object '
            + 'that lost its shape -- almost certainly one missing `additionalProperties: false`). '
            + 'Refusing to write it out.'
        );
    }

    const header = [
        '// GENERATED FILE -- do not edit by hand.',
        `// Source: Bpmn.Model ${schemaPath.match(/[/\\]([^/\\]+)[/\\]schema[/\\]/)?.[1] ?? 'unknown version'}, `
            + `payload format ${formatVersion}, schema/bpmn-payload.schema.json.`,
        '// Regenerate with: npm run generate:bpmn-types (from src/modules/Elsa.Studio.Workflows.Designer/ClientLib).',
        '// See scripts/generate-bpmn-types.js.',
        '',
    ].join('\n');

    return header + body;
}

async function main() {
    const checkMode = process.argv.includes('--check');
    const version = readPinnedVersion();
    const schemaPath = resolveSchemaPath(version);

    if (!fs.existsSync(schemaPath)) {
        const message = `Bpmn.Model ${version}'s schema was not found at ${schemaPath}. `
            + 'Restore it first, e.g.: dotnet restore src/modules/Elsa.Studio.Workflows.Designer/Elsa.Studio.Workflows.Designer.csproj';

        if (checkMode) {
            console.error(`error: ${message}`);
            process.exitCode = 1;
            return;
        }

        console.warn(`warning: ${message}\nwarning: leaving the already-generated ${path.relative(process.cwd(), OUTPUT_PATH)} untouched.`);
        return;
    }

    const generated = await generate(schemaPath);

    if (checkMode) {
        const existing = fs.existsSync(OUTPUT_PATH) ? fs.readFileSync(OUTPUT_PATH, 'utf8') : null;

        if (existing !== generated) {
            console.error(
                `error: ${path.relative(process.cwd(), OUTPUT_PATH)} is out of date with Bpmn.Model ${version}'s schema. `
                + 'Run `npm run generate:bpmn-types` and commit the result.'
            );
            process.exitCode = 1;
            return;
        }

        console.log(`${path.relative(process.cwd(), OUTPUT_PATH)} matches Bpmn.Model ${version}'s schema.`);
        return;
    }

    const existing = fs.existsSync(OUTPUT_PATH) ? fs.readFileSync(OUTPUT_PATH, 'utf8') : null;

    if (existing === generated) {
        console.log(`${path.relative(process.cwd(), OUTPUT_PATH)} is already up to date.`);
        return;
    }

    fs.mkdirSync(path.dirname(OUTPUT_PATH), { recursive: true });
    fs.writeFileSync(OUTPUT_PATH, generated);
    console.log(`Wrote ${path.relative(process.cwd(), OUTPUT_PATH)} from Bpmn.Model ${version}'s schema.`);
}

main().catch(error => {
    console.error(error.stack ?? String(error));
    process.exitCode = 1;
});
