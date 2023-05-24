import {PagedList} from "../../../models";
import {WorkflowDefinitionSummary} from "../models/entities";

export function updateSelectedWorkflowDefinitions(isChecked: boolean, workflowDefinitions: PagedList<WorkflowDefinitionSummary>, selectedWorkflowDefinitionIds: Array<string>) {
  const currentItems = workflowDefinitions.items.filter(item =>!item.isReadonly).map(x => x.definitionId);

  return isChecked
    ? selectedWorkflowDefinitionIds.concat(currentItems.filter(item => !selectedWorkflowDefinitionIds.includes(item)))
    : selectedWorkflowDefinitionIds.filter(item => !currentItems.includes(item));
}
