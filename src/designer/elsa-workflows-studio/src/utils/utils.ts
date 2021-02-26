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

export function timeSince(time) {
  switch (typeof time) {
    case 'number':
      break;
    case 'string':
      time = +new Date(time);
      break;
    case 'object':
      if (time.constructor === Date) time = time.getTime();
      break;
    default:
      time = +new Date();
  }
  const time_formats = [
    [60, 'seconds', 1], // 60
    [120, '1 minute ago', '1 minute from now'], // 60*2
    [3600, 'minutes', 60], // 60*60, 60
    [7200, '1 hour ago', '1 hour from now'], // 60*60*2
    [86400, 'hours', 3600], // 60*60*24, 60*60
    [172800, 'Yesterday', 'Tomorrow'], // 60*60*24*2
    [604800, 'days', 86400], // 60*60*24*7, 60*60*24
    [1209600, 'Last week', 'Next week'], // 60*60*24*7*4*2
    [2419200, 'weeks', 604800], // 60*60*24*7*4, 60*60*24*7
    [4838400, 'Last month', 'Next month'], // 60*60*24*7*4*2
    [29030400, 'months', 2419200], // 60*60*24*7*4*12, 60*60*24*7*4
    [58060800, 'Last year', 'Next year'], // 60*60*24*7*4*12*2
    [2903040000, 'years', 29030400], // 60*60*24*7*4*12*100, 60*60*24*7*4*12
    [5806080000, 'Last century', 'Next century'], // 60*60*24*7*4*12*100*2
    [58060800000, 'centuries', 2903040000] // 60*60*24*7*4*12*100*20, 60*60*24*7*4*12*100
  ];
  let seconds = (+new Date() - time) / 1000,
    token = 'ago',
    list_choice = 1;

  if (seconds === 0) {
    return 'Just now';
  }
  if (seconds < 0) {
    seconds = Math.abs(seconds);
    token = 'from now';
    list_choice = 2;
  }
  let i = 0,
    format;

  // tslint:disable-next-line: no-conditional-assignment
  while (format = time_formats[i++]) {
    if (seconds < format[0]) {
      if (typeof format[2] === 'string') {
        return format[list_choice];
      } else {
        return Math.floor(seconds / format[2]) + ' ' + format[1] + ' ' + token;
      }
    }
  }
  return time;
}
