// The drift guard generate-bpmn-types.js applies between Studio's Bpmn.Model pin and Core's
// (elsa-workflows/elsa-core#8906). Kept apart from the generator so its decisions can be tested
// against props content without touching the repository's own Directory.Packages.props files; see
// scripts/__tests__/bpmn-model-drift.test.ts.
'use strict';

const fs = require('fs');
const path = require('path');

const PACKAGE_ID = 'Bpmn.Model';
const VERSION_PROPERTY_NAME = 'BpmnModelVersion';
const DIVERGENCE_PROPERTY_NAME = 'BpmnModelDivergesFromCoreVersion';
const CORE_PIN_PATTERN = new RegExp(`<PackageVersion\\s+Include="${PACKAGE_ID.replace('.', '\\.')}"\\s+Version="([^"$]+)"`);

function readProperty(xml, name) {
    const match = xml.match(new RegExp(`<${name}>([^<]+)</${name}>`));
    return match ? match[1].trim() : null;
}

// Core's pin is the nearest Directory.Packages.props above Studio's that names a literal Bpmn.Model
// version (the root file in the monorepo layout).
function findCorePin(studioPropsPath) {
    let directory = path.dirname(path.dirname(studioPropsPath));

    while (true) {
        const candidate = path.join(directory, 'Directory.Packages.props');
        const match = fs.existsSync(candidate) && fs.readFileSync(candidate, 'utf8').match(CORE_PIN_PATTERN);

        if (match) {
            return { version: match[1], path: candidate };
        }

        const parent = path.dirname(directory);

        if (parent === directory) {
            return null;
        }

        directory = parent;
    }
}

// Returns null when Studio's pin is consistent with Core's (`core` as findCorePin returns it),
// otherwise the reason it is not.
function describeCoreDrift(studioPropsXml, studioPropsPath, core) {
    const studioVersion = readProperty(studioPropsXml, VERSION_PROPERTY_NAME);
    const divergence = readProperty(studioPropsXml, DIVERGENCE_PROPERTY_NAME);

    if (!core) {
        return `Could not find Core's ${PACKAGE_ID} pin in any Directory.Packages.props above ${studioPropsPath}.`;
    }

    if (studioVersion === core.version) {
        return divergence
            ? `<${DIVERGENCE_PROPERTY_NAME}> is set in ${studioPropsPath}, but Studio and Core both pin ${PACKAGE_ID} ${core.version}. Remove the stale override.`
            : null;
    }

    if (divergence === core.version) {
        return null;
    }

    return `Studio pins ${PACKAGE_ID} ${studioVersion} (${studioPropsPath}) but Core pins ${core.version} (${core.path}). `
        + `Move Studio's <${VERSION_PROPERTY_NAME}> to ${core.version} and regenerate, or, if the divergence is deliberate, `
        + `set <${DIVERGENCE_PROPERTY_NAME}>${core.version}</${DIVERGENCE_PROPERTY_NAME}> next to it with a comment explaining why.`;
}

// Drift fails --check and is only a warning during plain generation.
function driftSeverity(drift, checkMode) {
    if (!drift) {
        return null;
    }

    return checkMode ? 'error' : 'warning';
}

module.exports = {
    PACKAGE_ID,
    VERSION_PROPERTY_NAME,
    DIVERGENCE_PROPERTY_NAME,
    readProperty,
    findCorePin,
    describeCoreDrift,
    driftSeverity,
};
