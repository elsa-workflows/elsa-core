import { Component, h, Prop, State } from "@stencil/core";
import { RouterHistory } from "@stencil/router";
import {  eventBus } from "../../..";
import { WebhookDefinitionSummary } from "../../elsa-webhooks/models";
import Tunnel from "../../../data/dashboard";
import { createElsaSecretsClient, ElsaSecretsClient } from "../services/credential-manager.client";
import { SecretDescriptor, SecretModel } from "../models/secret.model";
import { SecretEventTypes } from "../models/secret.events";

@Component({
    tag: 'elsa-credential-manager-list-screen',
    shadow: false
})
export class CredentialManagerListScreen {
    @Prop() history?: RouterHistory;
    @Prop() serverUrl: string;
    @Prop() basePath: string;
    @Prop() culture: string;
    @State() secrets: Array<SecretModel> = []

    confirmDialog: HTMLElsaConfirmDialogElement;
    client: ElsaSecretsClient;

    async componentWillLoad() {
      await this.loadSecrets();
    }

    async connectedCallback() {
      eventBus.on(SecretEventTypes.SecretPicked, this.onSecretPicked);
      eventBus.on(SecretEventTypes.SecretUpdated, () => this.loadSecrets());

      window.addEventListener("message", this.listenToMessages);
    }

    async disconnectedCallback() {
      eventBus.detach(SecretEventTypes.SecretPicked, this.onSecretPicked);
      eventBus.detach(SecretEventTypes.SecretUpdated, () => this.loadSecrets());

      window.removeEventListener("message", this.listenToMessages);
    }

    listenToMessages = (e: MessageEvent) => {
      if (e.origin !== window.origin) {
        return;
      }
      if (e.data == "refreshSecrets") {
        this.loadSecrets();
      }
    }

    onSecretPicked = async args => {
      const secretDescriptor = args as any;
      const secretModel = this.newSecret(secretDescriptor);

      await this.showSecretEditorInternal(secretModel, false);
    };

    async onSecretEdit(e, secret) {
      const properties = secret.properties;
      const secretModel: SecretModel = {
        id: secret.id,
        displayName: secret.displayName,
        name: secret.name,
        type: secret.type,
        properties: this.mapProperties(properties)
      };

      await this.showSecretEditorInternal(secretModel, true);
    }

    mapProperties(properties) {
      return properties.map(prop => {
        return {
          expressions: {
            Literal: prop.expressions.Literal
          },
          name: prop.name
        }
      });
    }

    async showSecretEditorInternal(secret: SecretModel, animate: boolean) {
      await eventBus.emit(SecretEventTypes.SecretsEditor.Show, this, secret, animate);
    }

    newSecret(secretDescriptor: SecretDescriptor): any {
      const secret: SecretModel = {
        type: secretDescriptor.type,
        displayName: secretDescriptor.displayName,
        name: secretDescriptor.displayName,
        properties: [],
      };

      for (const property of secretDescriptor.inputProperties) {
        secret.properties[property.name] = {
          syntax: '',
          expression: '',
        };
      }

      return secret;
    }

    async onDeleteClick(e: Event, webhookDefinition: WebhookDefinitionSummary) {
      const result = await this.confirmDialog.show('Delete Secret', 'Are you sure you wish to permanently delete this secret?');

      if (!result)
        return;

      const elsaClient = await createElsaSecretsClient(this.serverUrl);
      await elsaClient.secretsApi.delete(webhookDefinition.id);
      await this.loadSecrets();
    }

    async loadSecrets() {
      const elsaClient = await createElsaSecretsClient(this.serverUrl);

      this.secrets = await elsaClient.secretsApi.list();
      await eventBus.emit(SecretEventTypes.SecretsLoaded, this, this.secrets);
    }

    render() {
      const secrets = this.secrets;

      return (
        <div>
          <div class="elsa-align-middle elsa-inline-block elsa-min-w-full elsa-border-b elsa-border-gray-200">
            <table class="elsa-min-w-full">
              <thead>
              <tr class="elsa-border-t elsa-border-gray-200">
                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider"><span class="lg:elsa-pl-2">Name</span></th>
                <th class="elsa-px-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-left elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider">
                  Type
                </th>
                <th class="elsa-pr-6 elsa-py-3 elsa-border-b elsa-border-gray-200 elsa-bg-gray-50 elsa-text-right elsa-text-xs elsa-leading-4 elsa-font-medium elsa-text-gray-500 elsa-uppercase elsa-tracking-wider"/>
              </tr>
              </thead>
              <tbody class="elsa-bg-white elsa-divide-y elsa-divide-gray-100">
              {secrets?.map(item => {

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
                        {item.name}
                      </div>
                    </td>

                    <td class="elsa-px-6 elsa-py-3 elsa-text-sm elsa-leading-5 elsa-text-gray-500 elsa-font-medium">
                      <div class="elsa-flex elsa-items-center elsa-space-x-3 lg:elsa-pl-2">
                        {item.type}
                      </div>
                    </td>

                    <td class="elsa-pr-6">
                      <elsa-context-menu history={this.history} menuItems={[
                        {text: 'Edit', clickHandler: e => this.onSecretEdit(e, item), icon: editIcon},
                        {text: 'Delete', clickHandler: e => this.onDeleteClick(e, item), icon: deleteIcon}
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
Tunnel.injectProps(CredentialManagerListScreen, ['serverUrl', 'culture', 'basePath']);
