import {Component, Event, EventEmitter, h, Host, Method, State} from '@stencil/core';
import {Container} from 'typedi';
import {Filter, FilterProps} from './filter';
import {ActivityDefinitionsApi} from "../services/api";
import {ActivityDefinitionsOrderBy, ActivityDefinitionSummary} from "../models";
import {PagedList, VersionOptions} from "../../../models";
import {DeleteIcon, EditIcon, PublishIcon, UnPublishIcon} from "../../../components/icons/tooling";
import {PagerData} from "../../../components/shared/pager/pager";
import {updateSelectedActivityDefinitions} from "../services/utils";
import {DefaultActions} from "../../../components/shared/modal-dialog";

@Component({
  tag: 'elsa-activity-definition-browser',
  shadow: false,
})
export class ActivityDefinitionBrowser {
  static readonly DEFAULT_PAGE_SIZE = 15;
  static readonly MIN_PAGE_SIZE = 5;
  static readonly MAX_PAGE_SIZE = 100;
  static readonly START_PAGE = 0;

  private activityDefinitionsApi: ActivityDefinitionsApi;
  private selectAllCheckbox: HTMLInputElement;

  @Event() public activityDefinitionSelected: EventEmitter<ActivityDefinitionSummary>;
  @Event() public newActivityDefinitionSelected: EventEmitter;
  @State() private activityDefinitions: PagedList<ActivityDefinitionSummary> = {items: [], totalCount: 0};
  @State() private publishedActivityDefinitions: PagedList<ActivityDefinitionSummary> = {items: [], totalCount: 0};
  @State() private selectedActivityDefinitionIds: Array<string> = [];
  @State() private currentPage: number = 0;
  @State() private currentPageSize: number = ActivityDefinitionBrowser.DEFAULT_PAGE_SIZE;
  @State() private orderBy?: ActivityDefinitionsOrderBy;
  @State() private labels?: string[];
  @State() private selectAllChecked: boolean;

  public async componentWillLoad() {

    this.activityDefinitionsApi = Container.get(ActivityDefinitionsApi);
    await this.loadActivityDefinitions();
  }

  private onNewDefinitionClick = async () => {
    this.newActivityDefinitionSelected.emit();
  };

  private async onPublishClick(e: MouseEvent, ActivityDefinition: ActivityDefinitionSummary) {
    await this.activityDefinitionsApi.publish(ActivityDefinition);
    await this.loadActivityDefinitions();
  }

  private async onUnPublishClick(e: MouseEvent, ActivityDefinition: ActivityDefinitionSummary) {
    await this.activityDefinitionsApi.retract(ActivityDefinition);
    await this.loadActivityDefinitions();
  }

  private async onDeleteClick(e: MouseEvent, ActivityDefinition: ActivityDefinitionSummary) {
    // const result = await this.confirmDialog.show(t('DeleteConfirmationModel.Title'), t('DeleteConfirmationModel.Message'));
    //
    // if (!result)
    //   return;

    await this.activityDefinitionsApi.delete(ActivityDefinition);
    await this.loadActivityDefinitions();
  }

  private onDeleteManyClick = async () => {
    await this.activityDefinitionsApi.deleteMany({definitionIds: this.selectedActivityDefinitionIds});
    await this.loadActivityDefinitions();
  };

  private onPublishManyClick = async () => {
    await this.activityDefinitionsApi.publishMany({definitionIds: this.selectedActivityDefinitionIds});
    await this.loadActivityDefinitions();
  };

  private onUnpublishManyClick = async () => {
    await this.activityDefinitionsApi.unpublishMany({definitionIds: this.selectedActivityDefinitionIds});
    await this.loadActivityDefinitions();
  };

  private onActivityDefinitionClick = async (e: MouseEvent, ActivityDefinition: ActivityDefinitionSummary) => {
    e.preventDefault();
    this.activityDefinitionSelected.emit(ActivityDefinition);
  };

  private async loadActivityDefinitions() {
    const publishedVersionOptions: VersionOptions = {isPublished: true};
    const api = this.activityDefinitionsApi;

    const latestActivityDefinitions = await api.list({
      page: this.currentPage,
      pageSize: this.currentPageSize,
      versionOptions: {isLatest: true},
      orderBy: this.orderBy,
      label: this.labels,
    });
    const unpublishedActivityDefinitionIds = latestActivityDefinitions.items.filter(x => !x.isPublished).map(x => x.definitionId);
    this.publishedActivityDefinitions = await api.list({
      definitionIds: unpublishedActivityDefinitionIds,
      versionOptions: publishedVersionOptions,
    });
    this.activityDefinitions = latestActivityDefinitions;
  }

  render() {
    const ActivityDefinitions = this.activityDefinitions;
    const publishedActivityDefinitions = this.publishedActivityDefinitions.items;
    const totalCount = ActivityDefinitions.totalCount;

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
        <div class="pt-4">
          <h2 class="text-lg font-medium ml-4 mb-2">Activity Definitions</h2>
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
                <th class="optional align-right">Latest Version</th>
                <th class="optional align-right">Published Version</th>
                <th/>
              </tr>
              </thead>
              <tbody>
              {ActivityDefinitions.items.map(definition => {
                const latestVersionNumber = definition.version;
                const {isPublished} = definition;
                const publishedVersion: ActivityDefinitionSummary = isPublished
                  ? definition
                  : publishedActivityDefinitions.find(x => x.definitionId == definition.definitionId);
                const publishedVersionNumber = !!publishedVersion ? publishedVersion.version : '-';

                const isSelected = this.selectedActivityDefinitionIds.findIndex(x => x === definition.definitionId) >= 0;
                let displayName = definition.displayName || definition.type;

                if (!displayName || displayName.trim().length == 0) displayName = 'Untitled';

                return (
                  <tr>
                    <td>
                      <input
                        type="checkbox"
                        value={definition.definitionId}
                        checked={isSelected}
                        onChange={e => this.onActivityDefinitionCheckChange(e, definition)}
                      />
                    </td>
                    <td>
                      <div class="flex items-center space-x-3 lg:pl-2">
                        <a onClick={e => this.onActivityDefinitionClick(e, definition)} href="#" class="truncate hover:text-gray-600">
                          <span>{displayName}</span>
                        </a>
                      </div>
                    </td>

                    <td class="optional align-right">{latestVersionNumber}</td>
                    <td class="optional align-right">{publishedVersionNumber}</td>
                    <td class="pr-6">
                      <elsa-context-menu
                        menuItems={[
                          {text: 'Edit', clickHandler: e => this.onActivityDefinitionClick(e, definition), icon: <EditIcon/>},
                          isPublished
                            ? {text: 'Unpublish', clickHandler: e => this.onUnPublishClick(e, definition), icon: <UnPublishIcon/>}
                            : {
                              text: 'Publish',
                              clickHandler: e => this.onPublishClick(e, definition),
                              icon: <PublishIcon/>,
                            },
                          {text: 'Delete', clickHandler: e => this.onDeleteClick(e, definition), icon: <DeleteIcon/>},
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

  onPaginated = async (e: CustomEvent<PagerData>) => {
    this.currentPage = e.detail.page;
    await this.loadActivityDefinitions();
  };

  private onPageSizeChanged = async (pageSize: number) => {
    this.currentPageSize = pageSize;
    this.resetPagination();
    await this.loadActivityDefinitions();
  };

  private onOrderByChanged = async (orderBy: ActivityDefinitionsOrderBy) => {
    this.orderBy = orderBy;
    await this.loadActivityDefinitions();
  };

  private onLabelChange = async (e: CustomEvent<Array<string>>) => {
    this.labels = e.detail;
    await this.loadActivityDefinitions();
  };

  private resetPagination = () => {
    this.currentPage = 0;
    this.selectedActivityDefinitionIds = [];
  };

  private getSelectAllState = () => {
    const {items} = this.activityDefinitions;
    const selectedWorkflowInstanceIds = this.selectedActivityDefinitionIds;
    return items.findIndex(item => !selectedWorkflowInstanceIds.includes(item.definitionId)) < 0;
  };

  private setSelectAllIndeterminateState = () => {
    if (this.selectAllCheckbox) {
      const selectedItems = this.activityDefinitions.items.filter(item => this.selectedActivityDefinitionIds.includes(item.definitionId));
      this.selectAllCheckbox.indeterminate = selectedItems.length != 0 && selectedItems.length != this.activityDefinitions.items.length;
    }
  };

  private onActivityDefinitionCheckChange(e: Event, ActivityDefinition: ActivityDefinitionSummary) {
    const checkBox = e.target as HTMLInputElement;
    const isChecked = checkBox.checked;

    if (isChecked) this.selectedActivityDefinitionIds = [...this.selectedActivityDefinitionIds, ActivityDefinition.definitionId];
    else this.selectedActivityDefinitionIds = this.selectedActivityDefinitionIds.filter(x => x != ActivityDefinition.definitionId);

    this.setSelectAllIndeterminateState();
  }

  private onSelectAllCheckChange(e: Event) {
    const checkBox = e.target as HTMLInputElement;
    const isChecked = checkBox.checked;
    this.selectAllChecked = isChecked;
    this.selectedActivityDefinitionIds = updateSelectedActivityDefinitions(isChecked, this.activityDefinitions, this.selectedActivityDefinitionIds);
  }
}
