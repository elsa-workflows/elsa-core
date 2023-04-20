import {FunctionalComponent, h} from "@stencil/core";
import {WorkflowDefinitionSummary} from "../../workflow-definitions/models/entities";
import {OrderBy, WorkflowStatus, WorkflowSubStatus} from "../../../models";
import {DropdownButtonItem, DropdownButtonOrigin} from "../../../components/shared/dropdown-button/models";
import {BulkActionsIcon, OrderByIcon, PageSizeIcon, WorkflowIcon, WorkflowStatusIcon} from "../../../components/icons/tooling";

export interface FilterProps extends BulkActionsProps {
  pageSizeFilter: PageSizeFilterProps;
  workflowFilter: WorkflowFilterProps;
  statusFilter: StatusFilterProps;
  subStatusFilter: SubStatusFilterProps;
  orderByFilter: OrderByFilterProps;
  resetFilter: () => void;
}

export interface BulkActionsProps {
  onBulkDelete: () => void;
  onBulkCancel: () => void;
}

export interface PageSizeFilterProps {
  selectedPageSize: number;
  onChange: (pageSize: number) => void;
}

export interface WorkflowFilterProps {
  workflows: Array<WorkflowDefinitionSummary>;
  selectedWorkflowDefinitionId?: string;
  onChange: (definitionId: string) => void;
}

export interface StatusFilterProps {
  selectedStatus?: WorkflowStatus;
  onChange: (status: WorkflowStatus) => void;
}

export interface SubStatusFilterProps {
  selectedStatus?: WorkflowSubStatus;
  onChange: (status: WorkflowSubStatus) => void;
}

export interface OrderByFilterProps {
  selectedOrderBy?: OrderBy;
  onChange: (orderBy: OrderBy) => void;
}

export const Filter: FunctionalComponent<FilterProps> = ({pageSizeFilter, workflowFilter, statusFilter, subStatusFilter, orderByFilter, resetFilter, onBulkDelete, onBulkCancel}) => {

  return <div class="p-8 flex content-end justify-right bg-white space-x-4">
    <div class="flex-shrink-0">
      <BulkActions onBulkDelete={onBulkDelete} onBulkCancel={onBulkCancel} />
    </div>
    <div class="flex-1">
      &nbsp;
    </div>
    <button onClick={resetFilter} type="button" class="text-sm text-blue-600 active:text-blue-700 px-3 active:ring ring-blue-500 rounded">
      Reset
    </button>
    <PageSizeFilter {...pageSizeFilter}/>
    <WorkflowFilter {...workflowFilter}/>
    <StatusFilter {...statusFilter}/>
    <SubStatusFilter {...subStatusFilter}/>
    <OrderByFilter {...orderByFilter} />
  </div>;
}

const BulkActions: FunctionalComponent<BulkActionsProps> = ({ onBulkDelete, onBulkCancel }) => {
  const bulkActions = [{
    text: 'Delete',
    name: 'Delete',
  }, {
    text: 'Cancel',
    name: 'Cancel',
  }];

  const onBulkActionSelected = (e: CustomEvent<DropdownButtonItem>) => {
    const action = e.detail;
    switch (action.name) {
      case 'Delete':
        onBulkDelete();
        break;
      case 'Cancel':
        onBulkCancel();
        break;

      default:
        action.handler();
    }
  };

  return <elsa-dropdown-button
    text="Bulk Actions" items={bulkActions} icon={<BulkActionsIcon/>}
    origin={DropdownButtonOrigin.TopLeft}
    theme="Secondary"
    onItemSelected={onBulkActionSelected}/>
}

const PageSizeFilter: FunctionalComponent<PageSizeFilterProps> = ({selectedPageSize, onChange}) => {
  const selectedPageSizeText = `Page size: ${selectedPageSize}`;
  const pageSizes: Array<number> = [5, 10, 15, 20, 30, 50, 100];

  const items: Array<DropdownButtonItem> = pageSizes.map(x => {
    const text = "" + x;
    return {text: text, isSelected: x == selectedPageSize, value: x};
  });

  const onPageSizeChanged = (e: CustomEvent<DropdownButtonItem>) => onChange(parseInt(e.detail.value));

  return <elsa-dropdown-button
    text={selectedPageSizeText} items={items} icon={<PageSizeIcon/>}
    origin={DropdownButtonOrigin.TopRight}
    theme="Secondary"
    onItemSelected={onPageSizeChanged}/>
}

const WorkflowFilter: FunctionalComponent<WorkflowFilterProps> = ({workflows, selectedWorkflowDefinitionId, onChange}) => {
  const selectedWorkflow = workflows.find(x => x.definitionId == selectedWorkflowDefinitionId);
  const getWorkflowName = (workflow?: WorkflowDefinitionSummary) => workflow?.name?.length > 0 ? workflow.name : 'Untitled';
  const selectedWorkflowText = !selectedWorkflowDefinitionId ? 'All workflows' : getWorkflowName(selectedWorkflow);
  let items: Array<DropdownButtonItem> = workflows.map(x => ({text: getWorkflowName(x), value: x.definitionId, isSelected: x.definitionId == selectedWorkflowDefinitionId}));
  const allItem: DropdownButtonItem = {text: 'All', value: null, isSelected: !selectedWorkflowDefinitionId};
  const onWorkflowChanged = (e: CustomEvent<DropdownButtonItem>) => onChange(e.detail.value);

  items = [allItem, ...items];

  return <elsa-dropdown-button
    text={selectedWorkflowText} items={items} icon={<WorkflowIcon/>}
    origin={DropdownButtonOrigin.TopRight}
    theme="Secondary"
    onItemSelected={onWorkflowChanged}/>
}

const StatusFilter: FunctionalComponent<StatusFilterProps> = ({selectedStatus, onChange}) => {
  const selectedStatusText = !!selectedStatus ? selectedStatus : 'Status';
  const statuses: Array<WorkflowStatus> = [null, WorkflowStatus.Running, WorkflowStatus.Finished];
  const statusOptions: Array<DropdownButtonItem> = statuses.map(x => ({text: x ?? 'All', isSelected: x == selectedStatus, value: x}));
  const onStatusChanged = (e: CustomEvent<DropdownButtonItem>) => onChange(e.detail.value);

  return <elsa-dropdown-button
    text={selectedStatusText} items={statusOptions} icon={<WorkflowStatusIcon/>}
    origin={DropdownButtonOrigin.TopRight}
    theme="Secondary"
    onItemSelected={onStatusChanged}/>
}

const SubStatusFilter: FunctionalComponent<SubStatusFilterProps> = ({selectedStatus, onChange}) => {
  const selectedSubStatusText = !!selectedStatus ? selectedStatus : 'Sub status';
  const subStatuses: Array<WorkflowSubStatus> = [null, WorkflowSubStatus.Executing, WorkflowSubStatus.Suspended, WorkflowSubStatus.Finished, WorkflowSubStatus.Faulted, WorkflowSubStatus.Cancelled];
  const subStatusOptions: Array<DropdownButtonItem> = subStatuses.map(x => ({text: x ?? 'All', isSelected: x == selectedStatus, value: x}));
  const onStatusChanged = (e: CustomEvent<DropdownButtonItem>) => onChange(e.detail.value);

  return <elsa-dropdown-button
    text={selectedSubStatusText} items={subStatusOptions} icon={<WorkflowStatusIcon/>}
    origin={DropdownButtonOrigin.TopRight}
    theme="Secondary"
    onItemSelected={onStatusChanged}/>
}

const OrderByFilter: FunctionalComponent<OrderByFilterProps> = ({selectedOrderBy, onChange}) => {
  const selectedOrderByText = !!selectedOrderBy ? `Ordered by: ${selectedOrderBy}` : 'Order by';
  const orderByValues: Array<OrderBy> = [OrderBy.Finished, OrderBy.LastExecuted, OrderBy.Created];
  const items: Array<DropdownButtonItem> = orderByValues.map(x => ({text: x, value: x, isSelected: x == selectedOrderBy}));
  const onOrderByChanged = (e: CustomEvent<DropdownButtonItem>) => onChange(e.detail.value);

  return <elsa-dropdown-button
    text={selectedOrderByText} items={items} icon={<OrderByIcon/>}
    theme="Secondary"
    origin={DropdownButtonOrigin.TopRight}
    onItemSelected={onOrderByChanged}/>
}
