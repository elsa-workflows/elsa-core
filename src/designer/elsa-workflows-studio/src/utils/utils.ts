import {WorkflowModel} from "../models/domain";

declare global {
  interface Array<T> {
    distinct(o: T): Array<T>;
  }
}

export function format(first: string, middle: string, last: string): string {
  return (first || '') + (middle ? ` ${middle}` : '') + (last ? ` ${last}` : '');
}

interface Array<T> {
  distinct(o: T): Array<T>;
}

Array.prototype.distinct = function () {
  return [...new Set(this)];
}

export function getChildActivities(workflowModel: WorkflowModel, parentId?: string) {
  if (parentId == null) {
    const targetIds = new Set(workflowModel.connections.map(x => x.targetId));
    return workflowModel.activities.filter(x => !targetIds.has(x.activityId));
  } else {
    const targetIds = new Set(workflowModel.connections.filter(x => x.sourceId == parentId).map(x => x.targetId));
    return workflowModel.activities.filter(x => targetIds.has(x.activityId));
  }
}
