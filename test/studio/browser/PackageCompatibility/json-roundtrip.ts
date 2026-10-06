import { createHash } from 'node:crypto';

export const workflowDefinitionSchema = 'https://elsaworkflows.io/schemas/workflow-definition/v3.0.0/schema.json';
export const maxWorkflowJsonBytes = 1024 * 1024;

type JsonObject = Record<string, unknown>;

const nonSemanticDefinitionMetadata = new Set([
  '$schema',
  'id',
  'version',
  'createdAt',
  'isLatest',
  'isPublished'
]);

export type WorkflowSemanticIdentity = {
  definitionId: string;
  rootId: string;
  activityIds: string[];
  outputName: string;
  outputValue: string;
  semanticSha256: string;
};

export type CheckedWorkflowJson = WorkflowSemanticIdentity & {
  document: JsonObject;
  documentSha256: string;
  bytes: number;
};

function object(value: unknown, failure: string): JsonObject {
  if (value === null || typeof value !== 'object' || Array.isArray(value))
    throw new Error(failure);
  return value as JsonObject;
}

function stringField(value: unknown, failure: string): string {
  if (typeof value !== 'string' || value.length === 0)
    throw new Error(failure);
  return value;
}

function literalString(value: unknown, failure: string): string {
  const expression = object(object(value, failure).expression, failure);
  if (expression.type !== 'Literal')
    throw new Error(failure);
  return stringField(expression.value, failure);
}

function canonicalJson(value: unknown): string {
  if (value === null || typeof value !== 'object') {
    const serialized = JSON.stringify(value);
    if (serialized === undefined)
      throw new Error('workflow_json_unsupported_value');
    return serialized;
  }

  if (Array.isArray(value))
    return '[' + value.map(canonicalJson).join(',') + ']';

  const record = value as JsonObject;
  const properties = Object.keys(record)
    .filter(key => record[key] !== undefined)
    .sort()
    .map(key => JSON.stringify(key) + ':' + canonicalJson(record[key]));
  return '{' + properties.join(',') + '}';
}

function semanticProjection(document: JsonObject): JsonObject {
  return Object.fromEntries(Object.entries(document).filter(([key]) => !nonSemanticDefinitionMetadata.has(key)));
}

function nestedActivities(activity: JsonObject, ids: string[], seen: Set<string>): void {
  const id = stringField(activity.id, 'workflow_activity_id_missing');
  stringField(activity.type, 'workflow_activity_type_missing');
  stringField(activity.nodeId, 'workflow_activity_node_id_missing');
  if (seen.has(id))
    throw new Error('workflow_activity_id_duplicate');
  seen.add(id);
  ids.push(id);

  if (activity.activities === undefined)
    return;

  if (!Array.isArray(activity.activities))
    throw new Error('workflow_nested_activities_invalid');
  for (const child of activity.activities)
    nestedActivities(object(child, 'workflow_nested_activity_invalid'), ids, seen);
}

export function workflowSemanticIdentity(value: unknown): WorkflowSemanticIdentity {
  const document = object(value, 'workflow_document_invalid');
  if (document.$schema !== undefined && document.$schema !== workflowDefinitionSchema)
    throw new Error('workflow_schema_unsupported');

  const definitionId = stringField(document.definitionId, 'workflow_definition_id_missing');
  const root = object(document.root, 'workflow_root_missing');
  const rootId = stringField(root.id, 'workflow_root_id_missing');
  stringField(root.type, 'workflow_root_type_missing');
  stringField(root.nodeId, 'workflow_root_node_id_missing');
  if (!Array.isArray(root.activities) || root.activities.length !== 1)
    throw new Error('workflow_candidate_activity_count_invalid');

  const activityIds: string[] = [];
  const seenIds = new Set([rootId]);
  for (const item of root.activities)
    nestedActivities(object(item, 'workflow_activity_invalid'), activityIds, seenIds);

  const activity = object(root.activities[0], 'workflow_activity_invalid');
  if (activity.type !== 'Elsa.SetOutput')
    throw new Error('workflow_candidate_output_activity_missing');
  const outputName = literalString(activity.outputName, 'workflow_output_name_literal_missing');
  const outputValue = literalString(activity.outputValue, 'workflow_output_value_literal_missing');

  if (!Array.isArray(document.outputs) || !document.outputs.some(item =>
    item !== null && typeof item === 'object' && !Array.isArray(item) && (item as JsonObject).name === outputName))
    throw new Error('workflow_candidate_output_unbound');

  const semanticSha256 = createHash('sha256').update(canonicalJson(semanticProjection(document))).digest('hex');
  return { definitionId, rootId, activityIds, outputName, outputValue, semanticSha256 };
}

export function readWorkflowJsonExport(raw: Buffer): CheckedWorkflowJson {
  if (!Buffer.isBuffer(raw) || raw.length === 0 || raw.length > maxWorkflowJsonBytes)
    throw new Error('workflow_json_export_size_invalid');

  let document: JsonObject;
  try {
    const text = new TextDecoder('utf-8', { fatal: true }).decode(raw);
    document = object(JSON.parse(text), 'workflow_json_export_invalid');
  } catch {
    throw new Error('workflow_json_export_invalid');
  }

  if (document.$schema !== workflowDefinitionSchema)
    throw new Error('workflow_schema_unsupported');

  const identity = workflowSemanticIdentity(document);
  return {
    ...identity,
    document,
    documentSha256: createHash('sha256').update(raw).digest('hex'),
    bytes: raw.length
  };
}
