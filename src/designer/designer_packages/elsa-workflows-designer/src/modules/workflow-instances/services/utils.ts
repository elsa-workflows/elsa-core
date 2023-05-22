import {PagedList, WorkflowInstanceSummary, WorkflowStatus, WorkflowSubStatus} from "../../../models";

const statusColorMap = {};
const subStatusColorMap = {};

statusColorMap[WorkflowStatus.Running] = 'bg-rose-600';
statusColorMap[WorkflowStatus.Finished] = 'tw-bg-green-600';

subStatusColorMap[WorkflowSubStatus.Executing] = 'bg-rose-600';
subStatusColorMap[WorkflowSubStatus.Suspended] = 'tw-bg-blue-600';
subStatusColorMap[WorkflowSubStatus.Finished] = 'tw-bg-green-600';
subStatusColorMap[WorkflowSubStatus.Faulted] = 'tw-bg-red-600';
subStatusColorMap[WorkflowSubStatus.Compensating] = 'bg-orange-600';
subStatusColorMap[WorkflowSubStatus.Cancelled] = 'bg-gray-900';

export function getStatusColor(status: WorkflowStatus) {
  return statusColorMap[status] ?? statusColorMap[WorkflowStatus.Running];
}

export function getSubStatusColor(status: WorkflowSubStatus) {
  return subStatusColorMap[status] ?? statusColorMap[WorkflowSubStatus.Suspended];
}

export function updateSelectedWorkflowInstances(isChecked: boolean, workflowInstances: PagedList<WorkflowInstanceSummary>, selectedWorkflowInstanceIds: Array<string>) {
  const currentItems = workflowInstances.items.map(x => x.id);

  return isChecked
    ? selectedWorkflowInstanceIds.concat(currentItems.filter(item => !selectedWorkflowInstanceIds.includes(item)))
    : selectedWorkflowInstanceIds.filter(item => !currentItems.includes(item));

}
