import { Component, Event, EventEmitter, h, Host, Method, Prop, State, Watch } from '@stencil/core';
import { DefaultActions, OrderBy, PagedList, VersionOptions, WorkflowDefinition, WorkflowDefinitionSummary } from '../../../models';
import { Container } from 'typedi';
import { ElsaApiClientProvider, ElsaClient } from '../../../services';
import { DeleteIcon, EditIcon, PublishIcon, UnPublishIcon } from '../../icons/tooling';
import { Filter, FilterProps } from './filter';
import { PagerData } from '../../shared/pager/pager';
import { updateSelectedWorkflowDefinitions } from './utils';
import { WorkflowDefinitionsOrderBy } from '../../../services/api-client/workflow-definitions-api';

@Component({
  tag: 'elsa-workflow-definition-browser',
  shadow: false,
})
export class WorkflowDefinitionBrowser {
  static readonly DEFAULT_PAGE_SIZE = 15;
  static readonly MIN_PAGE_SIZE = 5;
  static readonly MAX_PAGE_SIZE = 100;
  static readonly START_PAGE = 0;

  private elsaClient: ElsaClient;
  private modalDialog: HTMLElsaModalDialogElement;
  private selectAllCheckbox: HTMLInputElement;

  @Event() public workflowDefinitionSelected: EventEmitter<WorkflowDefinitionSummary>;
  @State() private workflowDefinitions: PagedList<WorkflowDefinitionSummary> = { items: [], totalCount: 0 };
  @State() private publishedWorkflowDefinitions: PagedList<WorkflowDefinitionSummary> = { items: [], totalCount: 0 };
  @State() private selectedWorkflowDefinitionIds: Array<string> = [];
  @State() private currentPage: number = 0;
  @State() private currentPageSize: number = WorkflowDefinitionBrowser.DEFAULT_PAGE_SIZE;
  @State() private orderBy?: WorkflowDefinitionsOrderBy;
  @State() private labels?: string[];
  @State() private selectAllChecked: boolean;
  @Method()
  public async show() {
    await this.modalDialog.show();
    await this.loadWorkflowDefinitions();
  }

  @Method()
  public async hide() {
    await this.modalDialog.hide();
  }

  public async componentWillLoad() {
    const elsaClientProvider = Container.get(ElsaApiClientProvider);
    this.elsaClient = await elsaClientProvider.getElsaClient();
  }

  private async onPublishClick(e: MouseEvent, workflowDefinition: WorkflowDefinitionSummary) {
    const elsaClient = this.elsaClient;
    await elsaClient.workflowDefinitions.publish(workflowDefinition);
    await this.loadWorkflowDefinitions();
  }

  private async onUnPublishClick(e: MouseEvent, workflowDefinition: WorkflowDefinitionSummary) {
    const elsaClient = this.elsaClient;
    await elsaClient.workflowDefinitions.retract(workflowDefinition);
    await this.loadWorkflowDefinitions();
  }

  private async onDeleteClick(e: MouseEvent, workflowDefinition: WorkflowDefinitionSummary) {
    // const result = await this.confirmDialog.show(t('DeleteConfirmationModel.Title'), t('DeleteConfirmationModel.Message'));
    //
    // if (!result)
    //   return;

    const elsaClient = this.elsaClient;
    await elsaClient.workflowDefinitions.delete(workflowDefinition);
    await this.loadWorkflowDefinitions();
  }

  private onDeleteManyClick = async () => {
    const elsaClient = this.elsaClient;
    await elsaClient.workflowDefinitions.deleteMany({ definitionIds: this.selectedWorkflowDefinitionIds });
    await this.loadWorkflowDefinitions();
  };

  private onPublishManyClick = async () => {
    const elsaClient = this.elsaClient;
    await elsaClient.workflowDefinitions.publishMany({ definitionIds: this.selectedWorkflowDefinitionIds });
    await this.loadWorkflowDefinitions();
  };

  private onUnpublishManyClick = async () => {
    const elsaClient = this.elsaClient;
    await elsaClient.workflowDefinitions.unpublishMany({ definitionIds: this.selectedWorkflowDefinitionIds });
    await this.loadWorkflowDefinitions();
  };

  private onWorkflowDefinitionClick = async (e: MouseEvent, workflowDefinition: WorkflowDefinitionSummary) => {
    e.preventDefault();
    this.workflowDefinitionSelected.emit(workflowDefinition);
    await this.hide();
  };

  private async loadWorkflowDefinitions() {
    const elsaClient = this.elsaClient;
    const latestVersionOptions: VersionOptions = { isLatest: true };
    const publishedVersionOptions: VersionOptions = { isPublished: true };

    // TODO: Load only json-based workflow definitions for now.
    // Later, also allow CLR-based workflows to be "edited" (publish / unpublish / position activities / set variables, etc.)
    const materializerName = 'Json';
    const latestWorkflowDefinitions = await elsaClient.workflowDefinitions.list({
      materializerName,
      page: this.currentPage,
      pageSize: this.currentPageSize,
      versionOptions: { isLatest: true },
      orderBy: this.orderBy,
      label: this.labels,
    });
    const unpublishedWorkflowDefinitionIds = latestWorkflowDefinitions.items.filter(x => !x.isPublished).map(x => x.definitionId);
    this.publishedWorkflowDefinitions = await elsaClient.workflowDefinitions.list({
      materializerName,
      definitionIds: unpublishedWorkflowDefinitionIds,
      versionOptions: publishedVersionOptions,
    });
    this.workflowDefinitions = latestWorkflowDefinitions;
  }

  render() {
    const workflowDefinitions = this.workflowDefinitions;
    const publishedWorkflowDefinitions = this.publishedWorkflowDefinitions.items;
    const totalCount = workflowDefinitions.totalCount;
    const closeAction = DefaultActions.Close();
    const actions = [closeAction];

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
      onBulkUnpublish: this.onUnpublishManyClick,
    };

    return (
      <Host class="block">
        <elsa-modal-dialog ref={el => (this.modalDialog = el)} actions={actions}>
          <div class="pt-4">
            <h2 class="text-lg font-medium ml-4 mb-2">Workflow Definitions</h2>
            <Filter {...filterProps} />
            <div class="align-middle inline-block min-w-full border-b border-gray-200">
              <table>
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
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {workflowDefinitions.items.map(workflowDefinition => {
                    const latestVersionNumber = workflowDefinition.version;
                    const { isPublished } = workflowDefinition;
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
                              { text: 'Edit', clickHandler: e => this.onWorkflowDefinitionClick(e, workflowDefinition), icon: <EditIcon /> },
                              isPublished
                                ? { text: 'Unpublish', clickHandler: e => this.onUnPublishClick(e, workflowDefinition), icon: <UnPublishIcon /> }
                                : {
                                    text: 'Publish',
                                    clickHandler: e => this.onPublishClick(e, workflowDefinition),
                                    icon: <PublishIcon />,
                                  },
                              { text: 'Delete', clickHandler: e => this.onDeleteClick(e, workflowDefinition), icon: <DeleteIcon /> },
                            ]}
                          />
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
              <elsa-pager page={this.currentPage} pageSize={this.currentPageSize} totalCount={totalCount} onPaginated={this.onPaginated} />
            </div>

            {/*<confirm-dialog ref={el => this.confirmDialog = el} culture={this.culture}/>*/}
          </div>
        </elsa-modal-dialog>
      </Host>
    );
  }

  onPaginated = async (e: CustomEvent<PagerData>) => {
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
    const { items } = this.workflowDefinitions;
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
}
