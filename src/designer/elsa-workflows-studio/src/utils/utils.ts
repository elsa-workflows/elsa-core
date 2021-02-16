import {ActivityDefinition, ActivityDefinitionProperty, ActivityModel, ConnectionModel, WorkflowModel} from "../models";

declare global {
  interface Array<T> {
    distinct(): Array<T>;
  }
}

export type Map<T> = {
  [key: string]: T
};

export function format(first: string, middle: string, last: string): string {
  return (first || '') + (middle ? ` ${middle}` : '') + (last ? ` ${last}` : '');
}

export interface Array<T> {
  distinct(): Array<T>;
  find<S extends T>(predicate: (this: void, value: T, index: number, obj: T[]) => value is S, thisArg?: any): S | undefined;
  find(predicate: (value: T, index: number, obj: T[]) => unknown, thisArg?: any): T | undefined;
  push(...items: T[]): number;
}

Array.prototype.distinct = function () {
  return [...new Set(this)];
}

export function getChildActivities(workflowModel: WorkflowModel, parentId?: string) {
  if (parentId == null) {
    const targetIds = new Set(workflowModel.connections.map(x => x.targetId));
    return workflowModel.activities.filter(x => !targetIds.has(x.activityId));
  } else {
    const targetIds = new Set(workflowModel.connections.filter(x => x.sourceId === parentId).map(x => x.targetId));
    return workflowModel.activities.filter(x => targetIds.has(x.activityId));
  }
}

export function getInboundConnections(workflowModel: WorkflowModel, activityId: string) {
  return workflowModel.connections.filter(x => x.targetId === activityId);
}

export function getOutboundConnections(workflowModel: WorkflowModel, activityId: string) {
  return workflowModel.connections.filter(x => x.sourceId === activityId);
}

export function removeActivity(workflowModel: WorkflowModel, activityId: string): WorkflowModel {
  const inboundConnections = getInboundConnections(workflowModel, activityId);
  const outboundConnections = getOutboundConnections(workflowModel, activityId);
  const connectionsToRemove = [...inboundConnections, ...outboundConnections];

  return {
    ...workflowModel,
    activities: workflowModel.activities.filter(x => x.activityId != activityId),
    connections: workflowModel.connections.filter(x => connectionsToRemove.indexOf(x) < 0)
  };
}

export function findActivity(workflowModel: WorkflowModel, activityId: string) {
  return workflowModel.activities.find(x => x.activityId === activityId);
}

export function addConnection(workflowModel: WorkflowModel, connection: ConnectionModel);
export function addConnection(workflowModel: WorkflowModel, sourceId: string, targetId: string, outcome: string);
export function addConnection(workflowModel: WorkflowModel, ...args: any) {

  const connection = typeof (args) == 'object' ? args as ConnectionModel : {sourceId: args[0], targetId: args[1], outcome: args[3]};

  return {
    ...workflowModel,
    connections: [...workflowModel.connections, connection]
  };
}

export function setActivityDefinitionProperty(activityDefinition: ActivityDefinition, name: string, expression: string, syntax: string) {
  setProperty(activityDefinition.properties, name, expression, syntax);
}

export function setActivityModelProperty(activityModel: ActivityModel, name: string, expression: string, syntax: string) {
  setProperty(activityModel.properties, name, expression, syntax);
}

export function setProperty(properties: Array<ActivityDefinitionProperty>, name: string, expression: string, syntax: string) {
  let property: ActivityDefinitionProperty = properties.find(x => x.name == name);

  if (!property) {
    property = {name: name, expression: expression, syntax: syntax};
    properties.push(property);
  } else {
    property.expression = expression;
    property.syntax = syntax;
  }
}

export function getProperty(properties: Array<ActivityDefinitionProperty>, name: string, defaultExpression?: () => string, defaultSyntax?: () => string): ActivityDefinitionProperty
{
  let property: ActivityDefinitionProperty = properties.find(x => x.name == name);

  if(!property)
    property = {name: name, expression: defaultExpression ? defaultExpression() : null, syntax: defaultSyntax ? defaultSyntax() : 'Literal'};

  return property;
}
