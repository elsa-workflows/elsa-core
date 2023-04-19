import { ListWorkflowDefinitionsRequest } from "./api";

const key = 'LS/wfDefinitionBrowser';

export function getRequest(): ListWorkflowDefinitionsRequest | undefined {
  var json = localStorage.getItem(key);

  if (!json) return;

  return JSON.parse(json);
}
export function persistRequest(request: ListWorkflowDefinitionsRequest) {
  localStorage.setItem(key, JSON.stringify(request));
}
