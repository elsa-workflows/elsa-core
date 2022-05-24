import {Component, h, Host, Prop, State, Event, EventEmitter, Watch} from '@stencil/core';
import {WorkflowDefinition, WorkflowDefinitionVersion} from "../../../../models";
import Tunnel from "../../../../data/dashboard";
import {createElsaClient} from "../../../../services";
import moment from "moment";
import {MenuItem} from "../../../controls/elsa-context-menu/models";

@Component({
  tag: 'elsa-version-history-panel',
  shadow: false
})
export class ElsaVersionHistoryPanel {

  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() serverUrl: string;
  @Event() versionSelected: EventEmitter<WorkflowDefinitionVersion>;
  @Event() deleteVersionClicked: EventEmitter<WorkflowDefinitionVersion>;
  @Event() revertVersionClicked: EventEmitter<WorkflowDefinitionVersion>;
  @State() versions: Array<WorkflowDefinitionVersion> = [];

  confirmDialog: HTMLElsaConfirmDialogElement;

  @Watch('workflowDefinition')
  async onWorkflowDefinitionChanged(value: WorkflowDefinition) {
    await this.loadVersions();
  }

  async componentWillLoad() {
    await this.loadVersions();
  }

  loadVersions = async () => {
    const client = await createElsaClient(this.serverUrl);
    const workflowDefinitionId = this.workflowDefinition.definitionId;
    this.versions = await client.workflowDefinitionsApi.getVersionHistory(workflowDefinitionId);
  }

  onViewVersionClick = (e: Event, version: WorkflowDefinitionVersion) => {
    e.preventDefault();
    this.versionSelected.emit(version);
  };

  onDeleteVersionClick = async (e: Event, version: WorkflowDefinitionVersion) => {
    e.preventDefault();

    const result = await this.confirmDialog.show('Delete Version', 'Are you sure you wish to permanently delete this version? This operation cannot be undone.');

    if (!result)
      return;

    this.deleteVersionClicked.emit(version);
  };

  onRevertVersionClick = (e: Event, version: WorkflowDefinitionVersion) => {
    e.preventDefault();
    this.revertVersionClicked.emit(version);
  };

  render() {

    const versions = this.versions;
    const selectedVersion = this.workflowDefinition.version;

    return (
      <Host>
        <dl class="elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
          <div class="elsa-text-sm elsa-font-medium">

            <div class="elsa-mt-2 elsa-flex elsa-flex-col">
              <div class="">
                <div class="elsa-inline-block elsa-min-w-full elsa-py-2 elsa-align-middle">
                  <div class="elsa-shadow-sm elsa-ring-1 elsa-ring-black elsa-ring-opacity-5 md:elsa-rounded-lg">
                    <table class="elsa-min-w-full elsa-divide-y elsa-divide-gray-300">
                      <thead class="elsa-bg-gray-50">
                      <tr>
                        <th scope="col" class="elsa-px-3 elsa-py-3.5 elsa-text-left elsa-text-sm elsa-font-semibold elsa-text-gray-900"/>
                        <th scope="col" class="elsa-py-3.5 elsa-pl-2 elsa-pr-3 elsa-text-left elsa-text-sm elsa-font-semibold elsa-text-gray-900">Version</th>
                        <th scope="col" class="elsa-px-3 elsa-py-3.5 elsa-text-left elsa-text-sm elsa-font-semibold elsa-text-gray-900">Created</th>
                        <th scope="col" class="elsa-relative elsa-py-3.5 elsa-pl-3 elsa-pr-4 sm:elsa-pr-6 lg:elsa-pr-8">
                          <span class="elsa-sr-only">View</span>
                        </th>
                        <th scope="col" class="elsa-relative elsa-py-3.5 elsa-pr-4 sm:elsa-pr-6 lg:elsa-pr-8"/>
                      </tr>
                      </thead>
                      <tbody class="elsa-divide-y elsa-divide-gray-200 elsa-bg-white">
                      {versions.map(v => {
                          const createdAt = moment(v.createdAt);
                          const isSelected = selectedVersion == v.version;
                          const rowCssClass = isSelected ? 'elsa-bg-gray-100' : undefined;
                          const canDeleteOrRevert = !v.isLatest && !v.isPublished;

                          const published = v.isPublished ? (
                            <div title="Published">
                              <svg class="elsa-h-6 elsa-w-6 elsa-text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                              </svg>
                            </div>

                          ) : undefined;

                          const deleteIcon = (
                            <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                              <path stroke="none" d="M0 0h24v24H0z"/>
                              <line x1="4" y1="7" x2="20" y2="7"/>
                              <line x1="10" y1="11" x2="10" y2="17"/>
                              <line x1="14" y1="11" x2="14" y2="17"/>
                              <path d="M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12"/>
                              <path d="M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3"/>
                            </svg>
                          );

                          const revertIcon = (
                            <svg class="elsa-h-6 elsa-w-6 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                              <path stroke="none" d="M0 0h24v24H0z"/>
                              <path d="M9 11l-4 4l4 4m-4 -4h11a4 4 0 0 0 0 -8h-1"/>
                            </svg>
                          );

                          const viewIcon = (
                            <svg class="elsa-h-6 w-6 elsa-text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                            </svg>

                          );

                          let contextMenuItems: Array<MenuItem> = [{
                            text: 'View',
                            icon: viewIcon,
                            clickHandler: e => this.onViewVersionClick(e, v)
                          }, {
                            text: 'Delete',
                            icon: deleteIcon,
                            clickHandler: e => this.onDeleteVersionClick(e, v)
                          },
                            {
                              text: 'Revert',
                              icon: revertIcon,
                              clickHandler: e => this.onRevertVersionClick(e, v)
                            }];

                          return (
                            <tr class={rowCssClass}>
                              <td class="elsa-whitespace-nowrap elsa-px-3 elsa-py-4 elsa-text-sm elsa-text-gray-500">{published}</td>
                              <td class="elsa-whitespace-nowrap elsa-py-4 elsa-pl-2 elsa-pr-3 elsa-text-sm elsa-font-medium elsa-text-gray-900">{v.version}</td>
                              <td class="elsa-whitespace-nowrap elsa-px-3 elsa-py-4 elsa-text-sm elsa-text-gray-500">{createdAt.format('DD-MM-YYYY HH:mm:ss')}</td>
                              <td class="elsa-relative elsa-whitespace-nowrap elsa-py-4 elsa-pl-3 elsa-pr-4 elsa-text-right elsa-text-sm elsa-font-medium sm:elsa-pr-6 lg:elsa-pr-8">
                                <button onClick={e => this.onViewVersionClick(e, v)}
                                        type="button"
                                        disabled={isSelected}
                                        class="elsa-inline-flex elsa-items-center elsa-rounded-md elsa-border elsa-border-gray-300 elsa-bg-white elsa-px-3 elsa-py-2 elsa-text-sm elsa-font-medium elsa-leading-4 elsa-text-gray-700 elsa-shadow-sm hover:elsa-bg-gray-50 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-blue-500 focus:elsa-ring-offset-2 disabled:elsa-cursor-not-allowed disabled:elsa-opacity-30">
                                  View
                                  <span class="elsa-sr-only">, {v.version}</span>
                                </button>
                              </td>
                              <td>
                                {v.isPublished || v.isPublished ? undefined : <elsa-context-menu menuItems={contextMenuItems}/>}
                              </td>
                            </tr>
                          );
                        }
                      )}

                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            </div>


          </div>
        </dl>
        <elsa-confirm-dialog ref={el => this.confirmDialog = el}/>
      </Host>
    );
  }
}

Tunnel.injectProps(ElsaVersionHistoryPanel, ['serverUrl']);
