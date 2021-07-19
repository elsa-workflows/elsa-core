import {Component, h, Prop, State} from '@stencil/core';
import * as collection from 'lodash/collection';
import {createElsaClient} from "../../../../services/elsa-client";
import {PagedList} from "../../../../models";
import {WebhookDefinitionSummary} from "../../../../models/webhook";
import {RouterHistory} from "@stencil/router";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-webhook-definitions-list-screen',
  shadow: false,
})
export class ElsaWebhookDefinitionsListScreen {
  @Prop() history?: RouterHistory;
  @Prop() serverUrl: string;
  @Prop() basePath: string;
  @Prop() culture: string;
  @State() webhookDefinitions: PagedList<WebhookDefinitionSummary> = {items: [], page: 1, pageSize: 50, totalCount: 0};

  confirmDialog: HTMLElsaConfirmDialogElement;

  async componentWillLoad() {
    await this.loadWebhookDefinitions();
  }

  async onDeleteClick(e: Event, webhookDefinition: WebhookDefinitionSummary) {
    const result = await this.confirmDialog.show('Delete Webhook Definition', 'Are you sure you wish to permanently delete this webhook?');

    if (!result)
      return;

    const elsaClient = this.createClient();
    await elsaClient.webhookDefinitionsApi.delete(webhookDefinition.id);
    await this.loadWebhookDefinitions();
  }

  async loadWebhookDefinitions() {
    const elsaClient = this.createClient();
    const page = 0;
    const pageSize = 50;
    this.webhookDefinitions = await elsaClient.webhookDefinitionsApi.list(page, pageSize);
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  render() {
    const webhookDefinitions = this.webhookDefinitions;
    const list = collection.orderBy(webhookDefinitions, 'name');
    const basePath = this.basePath;

    return (
      <div>
        <div class="elsa-align-middle elsa-inline-block elsa-min-w-full elsa-border-b elsa-border-gray-200">
          <table class="elsa-min-w-full">
            <thead>
            <tr class="elsa-border-t elsa-border-gray-200">
              <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider"><span class="lg:elsa-pl-2">Name</span></th>
              <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                Path
              </th>
              <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                Payload Type Name
              </th>
              <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                Enabled
              </th>              
              <th class="elsa-pr-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-right elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider"/>
            </tr>
            </thead>
            <tbody class="elsa-bg-white elsa-divide-y elsa-divide-gray-100">
            {collection.map(list, item => {
              
              const webhookDefinition: WebhookDefinitionSummary = item;
              let webhookDisplayName = webhookDefinition.name;

              if (!webhookDisplayName || webhookDisplayName.trim().length == 0)
              webhookDisplayName = webhookDefinition.name;

              if (!webhookDisplayName || webhookDisplayName.trim().length == 0)
              webhookDisplayName = 'Untitled';

              const editUrl = `${basePath}/webhook-definitions/${webhookDefinition.id}`;

              const editIcon = (
                <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
              );

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
              
              return (
                <tr>
                  <td class="elsa-px-6 elsa-py-3 elsa-whitespace-no-wrap elsa-text-sm elsa-leading-5 elsa-font-medium elsa-text-gray-900">
                    <div class="elsa-flex elsa-items-center elsa-space-x-3 lg:elsa-pl-2">
                      <stencil-route-link url={editUrl} anchorClass="elsa-truncate hover:elsa-text-gray-600"><span>{webhookDisplayName}</span></stencil-route-link>
                    </div>
                  </td>

                  <td class="elsa-px-6 elsa-py-3 elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-font-medium">
                    <div class="elsa-flex elsa-items-center elsa-space-x-3 lg:elsa-pl-2">
                      {webhookDefinition.path}
                    </div>
                  </td>

                  <td class="elsa-px-6 elsa-py-3 elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-font-medium">
                    <div class="elsa-flex elsa-items-center elsa-space-x-3 lg:elsa-pl-2">
                      {webhookDefinition.payloadTypeName}
                    </div>
                  </td>              

                  <td class="elsa-px-6 elsa-py-3 elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-font-medium">
                    <div class="elsa-flex elsa-items-center elsa-space-x-3 lg:elsa-pl-2">
                      {true == webhookDefinition.isEnabled ? 'Yes' : 'No'}
                    </div>
                  </td>   

                  <td class="elsa-pr-6">
                    <elsa-context-menu history={this.history} menuItems={[
                      {text: 'Edit', anchorUrl: editUrl, icon: editIcon},
                      {text: 'Delete', clickHandler: e => this.onDeleteClick(e, webhookDefinition), icon: deleteIcon}
                    ]}/>
                  </td>
                </tr>
              );
            })}
            </tbody>
          </table>
        </div>

        <elsa-confirm-dialog ref={el => this.confirmDialog = el}/>
      </div>
    );
  }
}
Tunnel.injectProps(ElsaWebhookDefinitionsListScreen, ['serverUrl', 'culture', 'basePath']);