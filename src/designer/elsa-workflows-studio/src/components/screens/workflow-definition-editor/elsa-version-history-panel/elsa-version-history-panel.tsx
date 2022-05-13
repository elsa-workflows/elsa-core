import {Component, h, Host, Prop, State, Event, EventEmitter, Watch} from '@stencil/core';
import {WorkflowDefinition, WorkflowDefinitionVersion} from "../../../../models";
import Tunnel from "../../../../data/dashboard";
import {createElsaClient} from "../../../../services";
import moment from "moment";

@Component({
  tag: 'elsa-version-history-panel',
  shadow: false
})
export class ElsaVersionHistoryPanel {

  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() serverUrl: string;
  @Event() versionSelected: EventEmitter<WorkflowDefinitionVersion>;
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

  render() {

    const versions = this.versions;
    const selectedVersion = this.workflowDefinition.version;

    return (
      <Host>
        <dl class="elsa-border-gray-200 elsa-divide-y elsa-divide-gray-200">
          <div class="elsa-text-sm elsa-font-medium">

            <div class="elsa-mt-2 elsa-flex elsa-flex-col">
              <div class="elsa-overflow-x-auto">
                <div class="elsa-inline-block elsa-min-w-full elsa-py-2 elsa-align-middle">
                  <div class="elsa-overflow-hidden elsa-shadow-sm elsa-ring-1 elsa-ring-black elsa-ring-opacity-5 md:elsa-rounded-lg">
                    <table class="elsa-min-w-full elsa-divide-y elsa-divide-gray-300">
                      <thead class="elsa-bg-gray-50">
                      <tr>
                        <th scope="col" class="elsa-py-3.5 elsa-pl-4 elsa-pr-3 elsa-text-left elsa-text-sm elsa-font-semibold elsa-text-gray-900 sm:elsa-pl-6 lg:elsa-pl-8">Version</th>
                        <th scope="col" class="elsa-px-3 elsa-py-3.5 elsa-text-left elsa-text-sm elsa-font-semibold elsa-text-gray-900">Created</th>
                        <th scope="col" class="elsa-px-3 elsa-py-3.5 elsa-text-left elsa-text-sm elsa-font-semibold elsa-text-gray-900">Published</th>
                        <th scope="col" class="elsa-relative elsa-py-3.5 elsa-pl-3 elsa-pr-4 sm:elsa-pr-6 lg:elsa-pr-8">
                          <span class="elsa-sr-only">View</span>
                        </th>
                      </tr>
                      </thead>
                      <tbody class="elsa-divide-y elsa-divide-gray-200 elsa-bg-white">
                      {versions.map(v => {
                          const createdAt = moment(v.createdAt);
                          const isSelected = selectedVersion == v.version;
                          const rowCssClass = isSelected ? 'elsa-bg-gray-100' : undefined;

                          const published = v.isPublished ? (
                            <svg class="elsa-h-6 elsa-w-6 elsa-text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                            </svg>

                          ) : undefined;

                          return (
                            <tr class={rowCssClass}>
                              <td class="elsa-whitespace-nowrap elsa-py-4 elsa-pl-4 elsa-pr-3 elsa-text-sm elsa-font-medium elsa-text-gray-900 sm:elsa-pl-6 lg:elsa-pl-8">{v.version}</td>
                              <td class="elsa-whitespace-nowrap elsa-px-3 elsa-py-4 elsa-text-sm elsa-text-gray-500">{createdAt.format('DD-MM-YYYY HH:mm:ss')}</td>
                              <td class="elsa-whitespace-nowrap elsa-px-3 elsa-py-4 elsa-text-sm elsa-text-gray-500">{published}</td>
                              <td class="elsa-relative elsa-whitespace-nowrap elsa-py-4 elsa-pl-3 elsa-pr-4 elsa-text-right elsa-text-sm elsa-font-medium sm:elsa-pr-6 lg:elsa-pr-8">
                                {!isSelected
                                  ? <a onClick={e => this.onViewVersionClick(e, v)} href="#" class="elsa-text-blue-600 hover:elsa-text-blue-900">View<span class="elsa-sr-only">, {v.version}</span></a>
                                  : undefined
                                }
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
