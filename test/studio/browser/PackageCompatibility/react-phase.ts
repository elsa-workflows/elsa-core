import { createHash } from 'node:crypto';
import { workflowSemanticIdentity } from './json-roundtrip.js';

export const reactAfterValue = 'synthetic-browser-react-value';
export const reactChecks = ['authentication', 'source_binding', 'react_mount', 'selection_callback', 'property_edit', 'saved', 'reloaded', 'identity_preserved', 'react_bundle', 'cleanup'] as const;
export const reactBundlePath = '/_content/Elsa.Studio.Workflows.Designer/react-designer.entry.js';
export type ReactHashes = { definition_id_sha256: string; root_id_sha256: string; activity_id_sha256: string; before_value_sha256: string };
export const reactHash = (value: string): string => createHash('sha256').update(value).digest('hex');

export function checkReactDefinition(document: any, expected: ReactHashes, changed: boolean): { definitionId: string; rootId: string; activityId: string; value: string } {
  const identity = workflowSemanticIdentity(document);
  if (![identity.definitionId, identity.rootId, identity.activityIds[0]].every(id => /^[0-9a-f]{1,16}$/.test(id)) ||
      document.root.type !== 'Elsa.Flowchart' || document.root.activities[0].outputValue.typeName !== 'Object' ||
      identity.outputName !== 'sentinel' || identity.activityIds.length !== 1 ||
      reactHash(identity.definitionId) !== expected.definition_id_sha256 ||
      reactHash(identity.rootId) !== expected.root_id_sha256 || reactHash(identity.activityIds[0]) !== expected.activity_id_sha256 ||
      reactHash(identity.outputValue) !== (changed ? reactHash(reactAfterValue) : expected.before_value_sha256) ||
      expected.before_value_sha256 === reactHash(reactAfterValue))
    throw new Error('react_workflow_binding_mismatch');
  return { definitionId: identity.definitionId, rootId: identity.rootId, activityId: identity.activityIds[0], value: identity.outputValue };
}
