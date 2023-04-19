import {Component, Event, EventEmitter, h, Host, Method, Prop, State, Watch} from '@stencil/core';
import {OrderBy, PagedList, VersionOptions} from '../../../models';
import {Container} from 'typedi';
import {DeleteIcon, EditIcon, PublishIcon, UnPublishIcon} from '../../../components/icons/tooling';
import {Filter, FilterProps} from './filter';
import {PagerData} from '../../../components/shared/pager/pager';
import {updateSelectedWorkflowDefinitions} from '../services/utils';
import {WorkflowDefinitionSummary} from "../models/entities";
import {ListWorkflowDefinitionsRequest, WorkflowDefinitionsApi, WorkflowDefinitionsOrderBy} from "../services/api";
import {ModalDialogService, DefaultModalActions, DefaultContents, ModalType} from "../../../components/shared/modal-dialog";
import {ActivityDescriptorManager} from "../../../services";
import { getRequest, persistRequest } from '../services/lookup-persistence';

@Component({
  tag: 'elsa-workflow-definition-browser',
  shadow: false,
})
export class WorkflowDefinitionBrowser {
  static readonly DEFAULT_PAGE_SIZE = 15;
  static readonly MIN_PAGE_SIZE = 5;
  static readonly MAX_PAGE_SIZE = 100;
  static readonly START_PAGE = 0;

  private readonly api: WorkflowDefinitionsApi;
  private selectAllCheckbox: HTMLInputElement;
  private readonly modalDialogService: ModalDialogService;
  private readonly activityDescriptorManager: ActivityDescriptorManager;

  constructor() {
    this.api = Container.get(WorkflowDefinitionsApi);
    this.modalDialogService = Container.get(ModalDialogService);
    this.activityDescriptorManager = Container.get(ActivityDescriptorManager);
  }

  @Event() workflowDefinitionSelected: EventEmitter<WorkflowDefinitionSummary>;
  @Event() public newWorkflowDefinitionSelected: EventEmitter;
  @State() private workflowDefinitions: PagedList<WorkflowDefinitionSummary> = {items: [], totalCount: 0};
  @State() private publishedWorkflowDefinitions: PagedList<WorkflowDefinitionSummary> = {items: [], totalCount: 0};
  @State() private selectedWorkflowDefinitionIds: Array<string> = [];
  @State() private currentPage: number = 0;
  @State() private currentPageSize: number = WorkflowDefinitionBrowser.DEFAULT_PAGE_SIZE;
  @State() private orderBy?: WorkflowDefinitionsOrderBy;
  @State() private labels?: string[];
  @State() private selectAllChecked: boolean;

  async componentWillLoad() {
    var persistedRequest = getRequest()

    if (persistedRequest) {
      this.currentPage = persistedRequest.page
      this.currentPageSize = persistedRequest.pageSize
      this.orderBy = persistedRequest.orderBy
    }

    await this.loadWorkflowDefinitions();
  }

  private onNewDefinitionClick = async () => {
    this.newWorkflowDefinitionSelected.emit();
  };

  private async onPublishClick(e: MouseEvent, workflowDefinition: WorkflowDefinitionSummary) {
    await this.api.publish(workflowDefinition);
    await this.loadWorkflowDefinitions();
  }

  private async onUnPublishClick(e: MouseEvent, workflowDefinition: WorkflowDefinitionSummary) {
    await this.api.retract(workflowDefinition);
    await this.loadWorkflowDefinitions();
  }

  private async onDeleteClick(e: MouseEvent, workflowDefinition: WorkflowDefinitionSummary) {
    this.modalDialogService.show(
      () => DefaultContents.Warning("Are you sure you want to delete this workflow definition?"),
      {
        actions: [DefaultModalActions.Delete(async () => {
          await this.api.delete(workflowDefinition);
          await this.loadWorkflowDefinitions();
          await this.activityDescriptorManager.refresh();
        }), DefaultModalActions.Cancel()],
        modalType: ModalType.Confirmation
      });
  }

  private onDeleteManyClick = async () => {
    this.modalDialogService.show(
      () => DefaultContents.Warning("Are you sure you want to delete selected workflow definitions?"),
      {
        actions: [DefaultModalActions.Delete(async () => {
          await this.api.deleteMany({definitionIds: this.selectedWorkflowDefinitionIds});
          await this.loadWorkflowDefinitions();
          await this.activityDescriptorManager.refresh();
        }), DefaultModalActions.Cancel()],
        modalType: ModalType.Confirmation
      });
  };

  private onPublishManyClick = async () => {
    this.modalDialogService.show(
      () => DefaultContents.Warning("Are you sure you want to publish selected workflow definitions?"),
      {
        actions: [DefaultModalActions.Publish(async () => {
          await this.api.publishMany({definitionIds: this.selectedWorkflowDefinitionIds});
          await this.loadWorkflowDefinitions();
        }), DefaultModalActions.Cancel()],
        modalType: ModalType.Confirmation
      });
  };

  private onUnpublishManyClick = async () => {
    this.modalDialogService.show(
      () => DefaultContents.Warning("Are you sure you want to unpublish selected workflow definitions?"),
      {
        actions: [DefaultModalActions.Unpublish(async () => {
          await this.api.unpublishMany({definitionIds: this.selectedWorkflowDefinitionIds});
          await this.loadWorkflowDefinitions();
        }), DefaultModalActions.Cancel()],
        modalType: ModalType.Confirmation
      });
  };

  private onWorkflowDefinitionClick = async (e: MouseEvent, workflowDefinition: WorkflowDefinitionSummary) => {
    e.preventDefault();
    this.workflowDefinitionSelected.emit(workflowDefinition);
  };

  private async loadWorkflowDefinitions() {

    // TODO: Load only json-based workflow definitions for now.
    // Later, also allow CLR-based workflows to be "edited" (publish / unpublish / position activities / set variables, etc.)
    const materializerName = 'Json';
    
    var request: ListWorkflowDefinitionsRequest = {
      materializerName,
      page: this.currentPage,
      pageSize: this.currentPageSize,
      versionOptions: {isLatest: true},
      orderBy: this.orderBy,
      label: this.labels,
    }
    persistRequest(request)

    const latestWorkflowDefinitions = await this.api.list(request);
    const unpublishedWorkflowDefinitionIds = latestWorkflowDefinitions.items.filter(x => !x.isPublished).map(x => x.definitionId);

    this.publishedWorkflowDefinitions = await this.api.list({
      materializerName,
      definitionIds: unpublishedWorkflowDefinitionIds,
      versionOptions: {isPublished: true},
    });

    this.workflowDefinitions = latestWorkflowDefinitions;
  }

  private onPaginated = async (e: CustomEvent<PagerData>) => {
    this.currentPage = e.detail.page;
    await this.loadWorkflowDefinitions();
  };

  private onPageSizeChanged = async (pageSize: number) => {
    this.currentPageSize = pageSize;
    this.resetPagination();
    await this.loadWorkflowDefinitions();
  };

  private onOrderByChanged = async (orderBy: WorkflowDefinitionsOrderBy) => {
    this.orderBy = orderBy;
    await this.loadWorkflowDefinitions();
  };

  private onLabelChange = async (e: CustomEvent<Array<string>>) => {
    this.labels = e.detail;
    await this.loadWorkflowDefinitions();
  };

  private resetPagination = () => {
    this.currentPage = 0;
    this.selectedWorkflowDefinitionIds = [];
  };

  private getSelectAllState = () => {
    const {items} = this.workflowDefinitions;
    const selectedWorkflowInstanceIds = this.selectedWorkflowDefinitionIds;
    return items.findIndex(item => !selectedWorkflowInstanceIds.includes(item.definitionId)) < 0;
  };

  private setSelectAllIndeterminateState = () => {
    if (this.selectAllCheckbox) {
      const selectedItems = this.workflowDefinitions.items.filter(item => this.selectedWorkflowDefinitionIds.includes(item.definitionId));
      this.selectAllCheckbox.indeterminate = selectedItems.length != 0 && selectedItems.length != this.workflowDefinitions.items.length;
    }
  };

  private onWorkflowDefinitionCheckChange(e: Event, workflowDefinition: WorkflowDefinitionSummary) {
    const checkBox = e.target as HTMLInputElement;
    const isChecked = checkBox.checked;

    if (isChecked) this.selectedWorkflowDefinitionIds = [...this.selectedWorkflowDefinitionIds, workflowDefinition.definitionId];
    else this.selectedWorkflowDefinitionIds = this.selectedWorkflowDefinitionIds.filter(x => x != workflowDefinition.definitionId);

    this.setSelectAllIndeterminateState();
  }

  private onSelectAllCheckChange(e: Event) {
    const checkBox = e.target as HTMLInputElement;
    const isChecked = checkBox.checked;
    this.selectAllChecked = isChecked;
    this.selectedWorkflowDefinitionIds = updateSelectedWorkflowDefinitions(isChecked, this.workflowDefinitions, this.selectedWorkflowDefinitionIds);
  }

  render() {
    const workflowDefinitions = this.workflowDefinitions;
    const publishedWorkflowDefinitions = this.publishedWorkflowDefinitions.items;
    const totalCount = workflowDefinitions.totalCount;

    const filterProps: FilterProps = {
      pageSizeFilter: {
        selectedPageSize: this.currentPageSize,
        onChange: this.onPageSizeChanged,
      },
      orderByFilter: {
        selectedOrderBy: this.orderBy,
        onChange: this.onOrderByChanged,
      },
      labelFilter: {
        selectedLabels: this.labels,
        onSelectedLabelsChanged: this.onLabelChange,
        buttonClass: 'text-gray-500 hover:text-gray-300',
        containerClass: 'mt-1.5',
      },
      onBulkDelete: this.onDeleteManyClick,
      onBulkPublish: this.onPublishManyClick,
      onBulkUnpublish: this.onUnpublishManyClick
    };

    return (
      <Host class="block">
        <div class="pt-4">
          <h2 class="text-lg font-medium ml-4 mb-2">Workflow Definitions</h2>
          <Filter {...filterProps} />
          <div class="align-middle inline-block min-w-full border-b border-gray-200">
            <table class="default-table">
              <thead>
              <tr>
                <th>
                  <input
                    type="checkbox"
                    value="true"
                    checked={this.getSelectAllState()}
                    onChange={e => this.onSelectAllCheckChange(e)}
                    ref={el => (this.selectAllCheckbox = el)}
                  />
                </th>
                <th>
                  <span class="lg:pl-2">Name</span>
                </th>
                <th>Instances</th>
                <th class="optional align-right">Latest Version</th>
                <th class="optional align-right">Published Version</th>
                <th/>
              </tr>
              </thead>
              <tbody>
              {workflowDefinitions.items.map(workflowDefinition => {
                const latestVersionNumber = workflowDefinition.version;
                const {isPublished} = workflowDefinition;
                const publishedVersion: WorkflowDefinitionSummary = isPublished
                  ? workflowDefinition
                  : publishedWorkflowDefinitions.find(x => x.definitionId == workflowDefinition.definitionId);
                const publishedVersionNumber = !!publishedVersion ? publishedVersion.version : '-';

                const isSelected = this.selectedWorkflowDefinitionIds.findIndex(x => x === workflowDefinition.definitionId) >= 0;
                let workflowDisplayName = workflowDefinition.name;

                if (!workflowDisplayName || workflowDisplayName.trim().length == 0) workflowDisplayName = 'Untitled';

                return (
                  <tr>
                    <td>
                      <input
                        type="checkbox"
                        value={workflowDefinition.definitionId}
                        checked={isSelected}
                        onChange={e => this.onWorkflowDefinitionCheckChange(e, workflowDefinition)}
                      />
                    </td>
                    <td>
                      <div class="flex items-center space-x-3 lg:pl-2">
                        <a onClick={e => this.onWorkflowDefinitionClick(e, workflowDefinition)} href="#" class="truncate hover:text-gray-600">
                          <span>{workflowDisplayName}</span>
                        </a>
                      </div>
                    </td>

                    <td>
                      <div class="flex items-center space-x-3 lg:pl-2">
                        <a href="#" class="truncate hover:text-gray-600">
                          Instances
                        </a>
                      </div>
                    </td>

                    <td class="optional align-right">{latestVersionNumber}</td>
                    <td class="optional align-right">{publishedVersionNumber}</td>
                    <td class="pr-6">
                      <elsa-context-menu
                        menuItems={[
                          {text: 'Edit', handler: e => this.onWorkflowDefinitionClick(e, workflowDefinition), icon: <EditIcon/>},
                          isPublished
                            ? {text: 'Unpublish', handler: e => this.onUnPublishClick(e, workflowDefinition), icon: <UnPublishIcon/>}
                            : {
                              text: 'Publish',
                              handler: e => this.onPublishClick(e, workflowDefinition),
                              icon: <PublishIcon/>,
                            },
                          {text: 'Delete', handler: e => this.onDeleteClick(e, workflowDefinition), icon: <DeleteIcon/>},
                        ]}
                      />
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
}
