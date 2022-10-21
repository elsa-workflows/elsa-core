import {ActivityDefinition, ActivityDefinitionProperty, ActivityModel, ConnectionModel, WorkflowModel} from "../models";
import * as collection from 'lodash/collection';
import {Duration} from "moment";
import {createDocument} from "@stencil/core/mock-doc";

declare global {
  interface Array<T> {
    distinct(): Array<T>;

    last(): T;
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

  last(): T;

  find<S extends T>(predicate: (this: void, value: T, index: number, obj: T[]) => value is S, thisArg?: any): S | undefined;

  find(predicate: (value: T, index: number, obj: T[]) => unknown, thisArg?: any): T | undefined;

  push(...items: T[]): number;

}

Array.prototype.distinct = function () {
  return [...new Set(this)];
}

if (!Array.prototype.last) {
  Array.prototype.last = function () {
    return this[this.length - 1];
  };
}

export function isNumeric(str: string): boolean {
  return !isNaN(str as any) && // use type coercion to parse the _entirety_ of the string (`parseFloat` alone does not do this)...
    !isNaN(parseFloat(str)) // ...and ensure strings of whitespace fail
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

export function removeConnection(workflowModel: WorkflowModel, sourceId: string, outcome: string): WorkflowModel {
  return {
    ...workflowModel,
    connections: workflowModel.connections.filter(x => !(x.sourceId === sourceId && x.outcome === outcome))
  };
}


export function findActivity(workflowModel: WorkflowModel, activityId: string) {
  return workflowModel.activities.find(x => x.activityId === activityId);
}

export function addConnection(workflowModel: WorkflowModel, connection: ConnectionModel) {
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

export function setProperty(properties: Array<ActivityDefinitionProperty>, name: string, expression: string, syntax?: string) {
  let property: ActivityDefinitionProperty = properties.find(x => x.name == name);

  if (!syntax)
    syntax = 'Literal';

  if (!property) {
    const expressions = {};
    expressions[syntax] = expression;
    property = {name: name, expressions: expressions, syntax: syntax};
    properties.push(property);
  } else {
    property.expressions[syntax] = expression;
    property.syntax = syntax;
  }
}

export function getOrCreateProperty(activity: ActivityModel, name: string, defaultExpression?: () => string, defaultSyntax?: () => string): ActivityDefinitionProperty {
  let property: ActivityDefinitionProperty = activity.properties.find(x => x.name == name);

  if (!property) {
    const expressions = {};
    let syntax = defaultSyntax ? defaultSyntax() : undefined;

    if (!syntax)
      syntax = 'Literal';

    expressions[syntax] = defaultExpression ? defaultExpression() : undefined;
    property = {name: name, expressions: expressions, syntax: null};
    activity.properties.push(property);
  }

  return property;
}

export function parseJson(json: string): any {
  if (!json)
    return null;

  try {
    return JSON.parse(json);
  } catch (e) {
    console.warn(`Error parsing JSON: ${e}`);
  }
  return undefined;
}

export function parseQuery(queryString?: string): any {

  if (!queryString)
    return {};

  const query = {};
  const pairs = (queryString[0] === '?' ? queryString.substr(1) : queryString).split('&');
  for (let i = 0; i < pairs.length; i++) {
    const pair = pairs[i].split('=');
    query[decodeURIComponent(pair[0])] = decodeURIComponent(pair[1] || '');
  }
  return query;
}

export function queryToString(query: any): string {
  const q = query || {};
  return collection.map(q, (v, k) => `${k}=${v}`).join('&');
}

export function mapSyntaxToLanguage(syntax: string): any {
  switch (syntax) {
    case 'Json':
      return 'json';
    case 'JavaScript':
      return 'javascript';
    case 'Liquid':
      return 'liquid';
    case 'SQL':
      return 'sql';
    case 'Literal':
    default:
      return 'plaintext';
  }
}

export function htmlToElement(html: string): Element {
  const template = document.createElement('template');
  html = html.trim(); // Never return a text node of whitespace as the result.
  template.innerHTML = html;
  return template.content.firstElementChild;
}

export function htmlDecode(value) {
  const textarea = htmlToElement("<textarea/>");
  textarea.innerHTML = value;
  return textarea.textContent;
}

export function htmlEncode(value) {
  const textarea = htmlToElement("<textarea/>");
  textarea.textContent = value;
  return textarea.innerHTML;
}

export function durationToString(duration: Duration) {
  return !!duration ? duration.asHours() > 1
      ? `${duration.asHours().toFixed(3)} h`
      : duration.asMinutes() > 1
        ? `${duration.asMinutes().toFixed(3)} m`
        : duration.asSeconds() > 1
          ? `${duration.asSeconds().toFixed(3)} s`
          : `${duration.asMilliseconds()} ms`
    : null;
}

export function clip(el) {
  const range = document.createRange();
  range.selectNodeContents(el);
  const sel = window.getSelection();
  sel.removeAllRanges();
  sel.addRange(range);
}

export async function awaitElement(selector) {
  while (document.querySelector(selector) === null) {
    await new Promise(resolve => requestAnimationFrame(resolve))
  }
  return document.querySelector(selector);
}

export interface Hash<TValue> {
  [key: string]: TValue;
}

export const stripActivityNameSpace = (name: string): string => {
  const lastDotIndex = name.lastIndexOf('.');
  return lastDotIndex < 0 ? name : name.substr(lastDotIndex + 1);
};

