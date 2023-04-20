import {Component, Event, EventEmitter, h, Host, Method, State} from '@stencil/core';
import _ from 'lodash';
import {Search} from "./search";
import {Filter, FilterProps} from "./filter";
import {ListWorkflowInstancesRequest, WorkflowInstancesApi} from "../services/workflow-instances-api";
import {getRequest, persistRequest} from '../services/lookup-persistence';
import {WorkflowDefinitionSummary} from "../../workflow-definitions/models/entities";
import {OrderBy, OrderDirection, PagedList, VersionOptions, WorkflowInstanceSummary, WorkflowStatus, WorkflowSubStatus} from "../../../models";
import {Container} from "typedi";
import {WorkflowDefinitionsApi} from "../../workflow-definitions/services/api";
import {getSubStatusColor, updateSelectedWorkflowInstances} from "../services/utils";
import {formatTimestamp} from "../../../utils";
import {DeleteIcon, EditIcon} from "../../../components/icons/tooling";
import {PagerData} from "../../../components/shared/pager/pager";
import {DefaultContents, DefaultModalActions, ModalDialogService, ModalType} from '../../../components/shared/modal-dialog';

@Component({
  tag: 'elsa-workflow-instance-browser',
  shadow: false,
})
export class WorkflowInstanceBrowser {
  static readonly DEFAULT_PAGE_SIZE = 15;
  static readonly MIN_PAGE_SIZE = 5;
  static readonly MAX_PAGE_SIZE = 100;
  static readonly START_PAGE = 0;

  private readonly workflowInstancesApi: WorkflowInstancesApi;
  private readonly workflowDefinitionsApi: WorkflowDefinitionsApi;
  private readonly modalDialogService: ModalDialogService;

  private selectAllCheckbox: HTMLInputElement;
  private publishedOrLatestWorkflows: Array<WorkflowDefinitionSummary> = [];


  constructor() {
    this.workflowInstancesApi = Container.get(WorkflowInstancesApi);
    this.workflowDefinitionsApi = Container.get(WorkflowDefinitionsApi);
    this.modalDialogService = Container.get(ModalDialogService);
  }

  @Event() public workflowInstanceSelected: EventEmitter<WorkflowInstanceSummary>;
  @State() private workflowInstances: PagedList<WorkflowInstanceSummary> = {items: [], totalCount: 0};
  @State() private workflows: Array<WorkflowDefinitionSummary> = [];
  @State() private selectAllChecked: boolean;
  @State() private selectedWorkflowInstanceIds: Array<string> = [];
  @State() private searchTerm?: string;
  @State() private currentPage: number = 0;
  @State() private currentPageSize: number = WorkflowInstanceBrowser.DEFAULT_PAGE_SIZE;
  @State() private selectedWorkflowDefinitionId?: string;
  @State() private selectedStatus?: WorkflowStatus;
  @State() private selectedSubStatus?: WorkflowSubStatus;
  @State() private orderBy?: OrderBy;

  async componentWillLoad() {
    var persistedRequest = getRequest()

    if (persistedRequest) {
      // TODO: Persist search term, need to bind the value to the input
      // this.searchTerm = persistedRequest.searchTerm
      this.currentPage = persistedRequest.page
      this.currentPageSize = persistedRequest.pageSize
      this.orderBy = persistedRequest.orderBy
      this.selectedWorkflowDefinitionId = persistedRequest.definitionId
      this.selectedStatus = persistedRequest.status
      this.selectedSubStatus = persistedRequest.subStatus
    }

    await this.loadWorkflowDefinitions();
    await this.loadWorkflowInstances();
  }

  public render() {
    const publishedOrLatestWorkflows = this.publishedOrLatestWorkflows;
    const workflowInstances = this.workflowInstances;
    const totalCount = workflowInstances.totalCount

    const filterProps: FilterProps = {
      pageSizeFilter: {
        selectedPageSize: this.currentPageSize,
        onChange: this.onPageSizeChanged
      },
      orderByFilter: {
        selectedOrderBy: this.orderBy,
        onChange: this.onOrderByChanged
      },
      statusFilter: {
        selectedStatus: this.selectedStatus,
        onChange: this.onWorkflowStatusChanged
      },
      subStatusFilter: {
        selectedStatus: this.selectedSubStatus,
        onChange: this.onWorkflowSubStatusChanged
      },
      workflowFilter: {
        workflows: publishedOrLatestWorkflows,
        selectedWorkflowDefinitionId: this.selectedWorkflowDefinitionId,
        onChange: this.onWorkflowChanged
      },
      resetFilter: async () => {
        this.resetPagination();
        this.currentPageSize = WorkflowInstanceBrowser.DEFAULT_PAGE_SIZE;
        this.selectedStatus = undefined
        this.selectedSubStatus = undefined
        this.orderBy = undefined
        this.selectedWorkflowDefinitionId = undefined
        
        await this.loadWorkflowInstances();
      },
      onBulkDelete: this.onDeleteManyClick,
      onBulkCancel: this.onCancelManyClick
    };

    return (
      <Host class="block">

        <div class="pt-4">
          <h2 class="text-lg font-medium ml-4 mb-2">Workflow Instances</h2>

          <Search onSearch={this.onSearch}/>
          <Filter {...filterProps}/>

          <div class="align-middle inline-block min-w-full border-b border-gray-200">
            <table class="default-table">
              <thead>
              <tr>
                <th>
                  <input type="checkbox" value="true" checked={this.getSelectAllState()}
                         onChange={e => this.onSelectAllCheckChange(e)}
                         ref={el => this.selectAllCheckbox = el}/>
                </th>
                <th><span class="lg:pl-2">ID</span></th>
                <th class="optional">Correlation</th>
                <th>Workflow</th>
                <th class="align-right">Version</th>
                <th class="optional">Name</th>
                <th>Status</th>
                <th class="optional">Created</th>
                <th class="optional">Finished</th>
                <th/>
              </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-100">
              {workflowInstances.items.map(workflowInstance => {
                const statusColor = getSubStatusColor(workflowInstance.subStatus);
                const isSelected = this.selectedWorkflowInstanceIds.findIndex(x => x === workflowInstance.id) >= 0;
                const workflow: WorkflowDefinitionSummary = publishedOrLatestWorkflows.find(x => x.definitionId == workflowInstance.definitionId);
                const workflowName = !!workflow ? (workflow.name || 'Untitled') : '(Definition not found)';

                return (
                  <tr>
                    <td>
                      <input type="checkbox" value={workflowInstance.id} checked={isSelected} onChange={e => this.onWorkflowInstanceCheckChange(e, workflowInstance)}/>
                    </td>
                    <td>
                      <div class="flex items-center space-x-3 lg:pl-2">
                        <a onClick={e => this.onWorkflowInstanceClick(e, workflowInstance)} href="#" class="truncate hover:text-gray-600"><span>{workflowInstance.id}</span></a>
                      </div>
                    </td>

                    <td class="optional">{workflowInstance.correlationId}</td>
                    <td class="optional">{workflowName}</td>
                    <td class="align-right">{workflowInstance.version}</td>
                    <td class="optional">{workflowInstance.name}</td>
                    <td>
                      <div class="flex items-center space-x-3 lg:pl-2">
                        <div class={`flex-shrink-0 w-2.5 h-2.5 rounded-full ${statusColor}`}/>
                        <span>{workflowInstance.status}</span>
                      </div>
                    </td>
                    <td class="optional">{formatTimestamp(workflowInstance.createdAt, '-')}</td>
                    <td class="optional">{formatTimestamp(workflowInstance.finishedAt, '-')}</td>
                    <td class="pr-6">
                      <elsa-context-menu menuItems={[
                        {text: 'Edit', handler: e => this.onWorkflowInstanceClick(e, workflowInstance), icon: <EditIcon/>},
                        {text: 'Delete', handler: e => this.onDeleteClick(e, workflowInstance), icon: <DeleteIcon/>}
                      ]}/>
                    </td>
                  </tr>
                );
              })}
              </tbody>
            </table>
            <elsa-pager page={this.currentPage} pageSize={this.currentPageSize} totalCount={totalCount} onPaginated={this.onPaginated}/>
          </div>
        </div>
      </Host>
    );
  }

  private onDeleteManyClick = async () => {
    if(this.selectedWorkflowInstanceIds.length == 0) 
      return;

    this.modalDialogService.show(
      () => DefaultContents.Warning("Are you sure you want to delete selected workflow instances?"),
      {
        actions: [DefaultModalActions.Delete(async () => {
          await this.workflowInstancesApi.deleteMany({workflowInstanceIds: this.selectedWorkflowInstanceIds});
          await this.loadWorkflowInstances();
        }), DefaultModalActions.Cancel()],
        modalType: ModalType.Confirmation
      });
  };

  private onCancelManyClick = async () => {
    if(this.selectedWorkflowInstanceIds.length == 0) 
      return;

    this.modalDialogService.show(
      () => DefaultContents.Warning("Are you sure you want to cancel selected workflow instances?"),
      {
        actions: [DefaultModalActions.Yes(async () => {
          await this.workflowInstancesApi.cancelMany({workflowInstanceIds: this.selectedWorkflowInstanceIds});
          await this.loadWorkflowInstances();
        }), DefaultModalActions.Cancel()],
        modalType: ModalType.Confirmation
      });
  };

  private resetPagination = () => {
    this.currentPage = 0;
    this.selectedWorkflowInstanceIds = [];
  };

  private async loadWorkflowInstances() {

    const request: ListWorkflowInstancesRequest = {
      searchTerm: this.searchTerm,
      definitionId: this.selectedWorkflowDefinitionId,
      status: this.selectedStatus,
      subStatus: this.selectedSubStatus,
      orderBy: this.orderBy,
      orderDirection: OrderDirection.Descending,
      page: this.currentPage,
      pageSize: this.currentPageSize
    };

    persistRequest(request)
    this.workflowInstances = await this.workflowInstancesApi.list(request);
  }

  private loadWorkflowDefinitions = async () => {
    const versionOptions: VersionOptions = {allVersions: true};
    const workflows = await this.workflowDefinitionsApi.list({versionOptions});
    this.publishedOrLatestWorkflows = this.selectLatestWorkflows(workflows.items);
  };

  private getSelectAllState = () => {
    const {items} = this.workflowInstances;
    const selectedWorkflowInstanceIds = this.selectedWorkflowInstanceIds;
    return items.findIndex(item => !selectedWorkflowInstanceIds.includes(item.id)) < 0;
  }

  private setSelectAllIndeterminateState = () => {
    if (this.selectAllCheckbox) {
      const selectedItems = this.workflowInstances.items.filter(item => this.selectedWorkflowInstanceIds.includes(item.id));
      this.selectAllCheckbox.indeterminate = selectedItems.length != 0 && selectedItems.length != this.workflowInstances.items.length;
    }
  }

  private selectLatestWorkflows = (workflows: Array<WorkflowDefinitionSummary>): Array<WorkflowDefinitionSummary> => {
    const groups = _.groupBy(workflows, 'definitionId');
    return _.map(groups, x => _.first(_.orderBy(x, 'version', 'desc')));
  }

  private onSearch = async (term: string) => {
    this.searchTerm = term;
    this.resetPagination();
    await this.loadWorkflowInstances();
  };

  private onPageSizeChanged = async (pageSize: number) => {
    this.currentPageSize = pageSize;
    this.resetPagination();
    await this.loadWorkflowInstances();
  };

  private onWorkflowChanged = async (definitionId: string) => {
    this.selectedWorkflowDefinitionId = definitionId;
    this.resetPagination();
    await this.loadWorkflowInstances();
  };

  private onWorkflowStatusChanged = async (status: WorkflowStatus) => {
    this.selectedStatus = status;
    this.resetPagination();
    await this.loadWorkflowInstances();
  };

  private onWorkflowSubStatusChanged = async (status: WorkflowSubStatus) => {
    this.selectedSubStatus = status;
    this.resetPagination();
    await this.loadWorkflowInstances();
  };

  private onOrderByChanged = async (orderBy: OrderBy) => {
    this.orderBy = orderBy;
    await this.loadWorkflowInstances();
  };

  private async onDeleteClick(e: MouseEvent, workflowInstance: WorkflowInstanceSummary) {
    // const result = await this.confirmDialog.show(t('DeleteConfirmationModel.Title'), t('DeleteConfirmationModel.Message'));
    //
    // if (!result)
    //   return;
    //

    await this.workflowInstancesApi.delete(workflowInstance);
    await this.loadWorkflowInstances();
  }

  private onWorkflowInstanceClick = async (e: MouseEvent, workflowInstance: WorkflowInstanceSummary) => {
    e.preventDefault();
    this.workflowInstanceSelected.emit(workflowInstance);
  }

  private onSelectAllCheckChange(e: Event) {
    const checkBox = e.target as HTMLInputElement;
    const isChecked = checkBox.checked;
    this.selectAllChecked = isChecked;
    this.selectedWorkflowInstanceIds = updateSelectedWorkflowInstances(isChecked, this.workflowInstances, this.selectedWorkflowInstanceIds);
  }

  private onWorkflowInstanceCheckChange(e: Event, workflowInstance: WorkflowInstanceSummary) {
    const checkBox = e.target as HTMLInputElement;
    const isChecked = checkBox.checked;

    if (isChecked)
      this.selectedWorkflowInstanceIds = [...this.selectedWorkflowInstanceIds, workflowInstance.id];
    else
      this.selectedWorkflowInstanceIds = this.selectedWorkflowInstanceIds.filter(x => x != workflowInstance.id);

    this.setSelectAllIndeterminateState();
  }

  onPaginated = async (e: CustomEvent<PagerData>) => {
    this.currentPage = e.detail.page;
    await this.loadWorkflowInstances();
  };
}
