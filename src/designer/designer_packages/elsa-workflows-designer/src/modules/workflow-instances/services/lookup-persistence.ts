import { ListWorkflowInstancesRequest, WorkflowInstancesApi } from './workflow-instances-api';

const key = 'LS/wfInstanceBrowser';

export function getRequest(): ListWorkflowInstancesRequest | undefined {
  var json = localStorage.getItem(key);

  if (!json) return;

  return JSON.parse(json);
}
export function persistRequest(request: ListWorkflowInstancesRequest) {
  localStorage.setItem(key, JSON.stringify(request));
}
