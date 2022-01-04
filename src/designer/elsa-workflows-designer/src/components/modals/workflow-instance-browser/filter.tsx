import {FunctionalComponent, h} from "@stencil/core";
import {BulkActionsIcon, OrderByIcon, PageSizeIcon, WorkflowIcon, WorkflowStatusIcon} from "../../icons/tooling";
import {DropdownButtonItem, DropdownButtonOrigin} from "../../shared/dropdown-button/models";
import {OrderBy, WorkflowStatus, WorkflowSummary} from "../../../models";

export interface FilterProps {
  pageSizeFilter: PageSizeFilterProps;
  workflowFilter: WorkflowFilterProps;
  statusFilter: StatusFilterProps;
  orderByFilter: OrderByFilterProps;
}

export interface PageSizeFilterProps {
  selectedPageSize: number;
  onChange: (pageSize: number) => void;
}

export interface WorkflowFilterProps {
  workflows: Array<WorkflowSummary>;
  selectedWorkflowDefinitionId?: string;
  onChange: (definitionId: string) => void;
}

export interface StatusFilterProps {
  selectedStatus?: WorkflowStatus;
  onChange: (status: WorkflowStatus) => void;
}

export interface OrderByFilterProps {
  selectedOrderBy?: OrderBy;
  onChange: (orderBy: OrderBy) => void;
}

export const Filter: FunctionalComponent<FilterProps> = ({pageSizeFilter, workflowFilter, statusFilter, orderByFilter}) => {

  return <div class="p-8 flex content-end justify-right bg-white space-x-4">
    <div class="flex-shrink-0">
      <BulkActions/>
    </div>
    <div class="flex-1">
      &nbsp;
    </div>
    <PageSizeFilter {...pageSizeFilter}/>
    <WorkflowFilter {...workflowFilter}/>
    <StatusFilter {...statusFilter}/>
    <OrderByFilter {...orderByFilter} />
  </div>;
}

const BulkActions: FunctionalComponent = () => {
  const bulkActions = [{
    text: 'Cancel',
    name: 'Cancel',
  }, {
    text: 'Delete',
    name: 'Delete',
  }];

  const onBulkActionSelected = (e: CustomEvent<DropdownButtonItem>) => {

  }

  return <elsa-dropdown-button
    text="Bulk Actions" items={bulkActions} icon={<BulkActionsIcon/>}
    origin={DropdownButtonOrigin.TopLeft}
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
    onItemSelected={onPageSizeChanged}/>
}

const WorkflowFilter: FunctionalComponent<WorkflowFilterProps> = ({workflows, selectedWorkflowDefinitionId, onChange}) => {
  const selectedWorkflow = workflows.find(x => x.definitionId == selectedWorkflowDefinitionId);
  const getWorkflowName = (workflow?: WorkflowSummary) => workflow?.name?.length > 0 ? workflow.name : 'Untitled';
  const selectedWorkflowText = !selectedWorkflowDefinitionId ? 'All workflows' : getWorkflowName(selectedWorkflow);
  let items: Array<DropdownButtonItem> = workflows.map(x => ({text: getWorkflowName(x), value: x.definitionId, isSelected: x.definitionId == selectedWorkflowDefinitionId}));
  const allItem: DropdownButtonItem = {text: 'All', value: null, isSelected: !selectedWorkflowDefinitionId};
  const onWorkflowChanged = (e: CustomEvent<DropdownButtonItem>) => onChange(e.detail.value);

  items = [allItem, ...items];

  return <elsa-dropdown-button
    text={selectedWorkflowText} items={items} icon={<WorkflowIcon/>}
    origin={DropdownButtonOrigin.TopRight}
    onItemSelected={onWorkflowChanged}/>
}

const StatusFilter: FunctionalComponent<StatusFilterProps> = ({selectedStatus, onChange}) => {
  const selectedStatusText = !!selectedStatus ? selectedStatus : 'Status';
  const statuses: Array<WorkflowStatus> = [null, WorkflowStatus.Running, WorkflowStatus.Suspended, WorkflowStatus.Finished, WorkflowStatus.Compensating, WorkflowStatus.Faulted, WorkflowStatus.Cancelled, WorkflowStatus.Idle];
  const items: Array<DropdownButtonItem> = statuses.map(x => ({text: x ?? 'All', isSelected: x == selectedStatus, value: x}));
  const onStatusChanged = (e: CustomEvent<DropdownButtonItem>) => onChange(e.detail.value);

  return <elsa-dropdown-button
    text={selectedStatusText} items={items} icon={<WorkflowStatusIcon/>}
    origin={DropdownButtonOrigin.TopRight}
    onItemSelected={onStatusChanged}/>
}

const OrderByFilter: FunctionalComponent<OrderByFilterProps> = ({selectedOrderBy, onChange}) => {
  const selectedOrderByText = !!selectedOrderBy ? `Ordered by: ${selectedOrderBy}` : 'Order by';
  const orderByValues: Array<OrderBy> = [OrderBy.Finished, OrderBy.LastExecuted, OrderBy.Created];
  const items: Array<DropdownButtonItem> = orderByValues.map(x => ({text: x, value: x, isSelected: x == selectedOrderBy}));
  const onOrderByChanged = (e: CustomEvent<DropdownButtonItem>) => onChange(e.detail.value);

  return <elsa-dropdown-button
    text={selectedOrderByText} items={items} icon={<OrderByIcon/>}
    origin={DropdownButtonOrigin.TopRight}
    onItemSelected={onOrderByChanged}/>
}
