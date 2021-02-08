import {ConnectionModel, WorkflowModel} from "../models/domain";

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

interface Array<T> {
  distinct(): Array<T>;
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
