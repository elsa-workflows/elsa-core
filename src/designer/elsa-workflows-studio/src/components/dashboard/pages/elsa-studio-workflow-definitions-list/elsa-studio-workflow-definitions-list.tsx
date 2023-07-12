import {Component, h, Prop, State} from '@stencil/core';
import { createElsaClient } from "../../../../services";
import {RouterHistory} from "@stencil/router";
import 'i18next-wc';
import {GetIntlMessage, IntlMessage} from "../../../i18n/intl-message";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {i18n} from "i18next";
import Tunnel from "../../../../data/dashboard";
import { leave, toggle } from 'el-transition'

@Component({
  tag: 'elsa-studio-workflow-definitions-list',
  shadow: false,
})
export class ElsaStudioWorkflowDefinitionsList {
  @Prop() history: RouterHistory;
  @Prop() culture: string;
  @Prop() basePath: string;
  @Prop({ attribute: 'server-url' }) serverUrl: string;
  private i18next: i18n;
  private fileInput: HTMLInputElement;
  private workflowDefinitionsListScreen: HTMLElsaWorkflowDefinitionsListScreenElement
  private menu: HTMLElement;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  async onFileInputChange(e: Event) {
    const files = this.fileInput.files;

    if (files.length == 0) {
      return;
    }
    const file = files[0];

    const elsaClient = await createElsaClient(this.serverUrl);

    try {
      await elsaClient.workflowDefinitionsApi.restore(file);
    } catch (e) {
      console.error(e);
    }
    await this.workflowDefinitionsListScreen.loadWorkflowDefinitions();
  }

  restoreWorkflows = async (e: Event) => {
    e.preventDefault();
    this.fileInput.value = null;
    this.fileInput.click();
    toggle(this.menu);
  }

  toggleMenu(e?: Event) {
    toggle(this.menu);
  }

  render() {
    const basePath = this.basePath;
    const IntlMessage = GetIntlMessage(this.i18next);

    return (
      <div>
        <div class="elsa-border-b elsa-border-gray-200 elsa-px-4 elsa-py-4 sm:elsa-flex sm:elsa-items-center sm:elsa-justify-between sm:elsa-px-6 lg:elsa-px-8 elsa-bg-white">
          <div class="elsa-flex-1 elsa-min-w-0">
            <h1 class="elsa-text-lg elsa-font-medium elsa-leading-6 elsa-text-gray-900 sm:elsa-truncate">
              <IntlMessage label="Title"/>
            </h1>
          </div>
          <div class="elsa-mt-4 elsa-flex sm:elsa-mt-0 sm:elsa-ml-4">
            <span class="elsa-relative elsa-z-20 elsa-inline-flex elsa-shadow-sm elsa-rounded-md">
              <stencil-route-link url={`${basePath}/workflow-definitions/new`}
                class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-transparent elsa-shadow-sm elsa-text-sm elsa-font-medium elsa-rounded-l-md elsa-text-white elsa-bg-blue-600 hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 focus:elsa-z-10">
                <IntlMessage label="CreateButton"/>
              </stencil-route-link>
              <span class="-elsa-ml-px elsa-relative elsa-block">
                <button onClick={() => this.toggleMenu()} id="option-menu" type="button"
                  class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-2 elsa-py-2 elsa-rounded-r-md elsa-border elsa-border-transparent elsa-bg-blue-600 elsa-text-sm elsa-font-medium elsa-text-white hover:elsa-bg-blue-700 focus:elsa-z-10 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-blue-500 focus:elsa-border-blue-500">
                  <span class="elsa-sr-only">Open options</span>
                  <svg class="elsa-h-5 elsa-w-5" x-description="Heroicon name: solid/chevron-down" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
                  </svg>
                </button>
                <div ref={el => this.menu = el}
                  data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
                  data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
                  data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
                  data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
                  data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
                  data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
                  class="hidden origin-top-left elsa-absolute elsa-right-0 elsa-top-10 elsa-mb-2 -elsa-mr-1 elsa-w-56 elsa-rounded-md elsa-shadow-lg elsa-bg-white elsa-ring-1 elsa-ring-black elsa-ring-opacity-5">
                  <div class="elsa-divide-y elsa-divide-gray-100 focus:elsa-outline-none" role="menu" aria-orientation="vertical" aria-labelledby="option-menu">

                    <div class="elsa-py-1" role="none">
                      <a href="#" onClick={(e) => this.restoreWorkflows(e)} class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900" role="menuitem">
                        <IntlMessage label="RestoreButton" />
                      </a>

                      <a href={`${basePath}/v1/workflow-definitions/backup`} onClick={(e) => this.toggleMenu(e)} class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900" role="menuitem">
                        <IntlMessage label="BackupButton" />
                      </a>

                    </div>
                  </div>
                </div>
              </span>
            </span>
          </div>
        </div>

        <elsa-workflow-definitions-list-screen ref={el => this.workflowDefinitionsListScreen = el} />
        <input type="file" class="hidden" onChange={(e) => this.onFileInputChange(e)} ref={el => this.fileInput = el} accept=".zip" />
      </div>
    );
  }
}
Tunnel.injectProps(ElsaStudioWorkflowDefinitionsList, ['serverUrl', 'culture', 'basePath']);
