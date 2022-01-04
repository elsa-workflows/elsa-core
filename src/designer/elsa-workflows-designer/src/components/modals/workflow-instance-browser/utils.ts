import {PagedList, WorkflowInstanceSummary, WorkflowStatus} from "../../../models";

const statusColorMap = {};

statusColorMap[WorkflowStatus.Idle] = 'bg-gray-600';
statusColorMap[WorkflowStatus.Running] = 'bg-rose-600';
statusColorMap[WorkflowStatus.Suspended] = 'bg-blue-600';
statusColorMap[WorkflowStatus.Finished] = 'bg-green-600';
statusColorMap[WorkflowStatus.Faulted] = 'bg-red-600';
statusColorMap[WorkflowStatus.Compensating] = 'bg-orange-600';
statusColorMap[WorkflowStatus.Cancelled] = 'bg-gray-900';

export function getStatusColor(status: WorkflowStatus) {
  return statusColorMap[status] ?? statusColorMap[WorkflowStatus.Idle];
}

export function updateSelectedWorkflowInstances(isChecked: boolean, workflowInstances: PagedList<WorkflowInstanceSummary>, selectedWorkflowInstanceIds: Array<string>) {
  const currentItems = workflowInstances.items.map(x => x.id);

  return isChecked
    ? selectedWorkflowInstanceIds.concat(currentItems.filter(item => !selectedWorkflowInstanceIds.includes(item)))
    : selectedWorkflowInstanceIds.filter(item => !currentItems.includes(item));

}
