import {Component, Event, EventEmitter, Host, h, Listen, Prop} from '@stencil/core';
import {leave, toggle} from 'el-transition'
import {WorkflowDefinition} from "../../../../models";
import Tunnel from '../../../../data/workflow-editor';
import {i18n} from "i18next";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";

@Component({
  tag: 'elsa-workflow-publish-button',
  shadow: false,
})
export class ElsaWorkflowPublishButton {

  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() publishing: boolean;
  @Prop() culture: string;
  @Event({bubbles: true}) publishClicked: EventEmitter;
  @Event({bubbles: true}) unPublishClicked: EventEmitter;
  @Event({bubbles: true}) revertClicked: EventEmitter;
  @Event({bubbles: true}) exportClicked: EventEmitter;
  @Event({bubbles: true}) importClicked: EventEmitter<File>;
  @Event({bubbles: true}) deleteClicked: EventEmitter;

  i18next: i18n;
  menu: HTMLElement;
  fileInput: HTMLInputElement;
  element: HTMLElement;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  @Listen('click', {target: 'window'})
  onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeMenu();
  }

  t = (key: string) => this.i18next.t(key);

  closeMenu() {
    leave(this.menu);
  }

  toggleMenu() {
    toggle(this.menu);
  }

  onPublishClick = (e: Event) => {
    e.preventDefault();
    this.publishClicked.emit();
    leave(this.menu);
  }

  onUnPublishClick = (e: Event) => {
    e.preventDefault();
    this.unPublishClicked.emit();
    leave(this.menu);
  }

  onRevertClick = (e: Event) => {
    e.preventDefault();
    this.revertClicked.emit();
    leave(this.menu);
  }

  onExportClick = async (e: Event) => {
    e.preventDefault();
    this.exportClicked.emit();
    leave(this.menu);
  }

  onImportClick = async (e: Event) => {
    e.preventDefault();
    this.fileInput.value = null;
    this.fileInput.click();

    leave(this.menu);
  }

  onDeleteClick = async (e: Event) => {
    e.preventDefault();

    this.deleteClicked.emit();

    leave(this.menu);
  }

  async onFileInputChange(e: Event) {
    const files = this.fileInput.files;

    if (files.length == 0) {
      return;
    }

    this.importClicked.emit(files[0]);
  }

  render() {
    const t = this.t;

    return (
      <Host class="elsa-block" ref={el => this.element = el}>
        <span class="elsa-relative elsa-z-0 elsa-inline-flex elsa-shadow-sm elsa-rounded-md">
          {this.renderMainButton()}
          <span class="-elsa-ml-px elsa-relative elsa-block">
            <button onClick={() => this.toggleMenu()} id="option-menu" type="button"
                    class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-2 elsa-py-2 elsa-rounded-r-md elsa-border elsa-border-gray-300 elsa-bg-white elsa-text-sm elsa-font-medium elsa-text-gray-500 hover:elsa-bg-gray-50 focus:elsa-z-10 focus:elsa-outline-none focus:elsa-ring-1 focus:elsa-ring-blue-500 focus:elsa-border-blue-500">
              <span class="elsa-sr-only">Open options</span>
              <svg class="elsa-h-5 elsa-w-5" x-description="Heroicon name: solid/chevron-down" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"/>
              </svg>
            </button>
            <div ref={el => this.menu = el}
                 data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
                 data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
                 data-transition-enter-end="elsa-transform elsa-opacity-100 elsa-scale-100"
                 data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
                 data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
                 data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
                 class="hidden origin-bottom-right elsa-absolute elsa-right-0 elsa-bottom-10 elsa-mb-2 -elsa-mr-1 elsa-w-56 elsa-rounded-md elsa-shadow-lg elsa-bg-white elsa-ring-1 elsa-ring-black elsa-ring-opacity-5">
              <div class="elsa-divide-y elsa-divide-gray-100 focus:elsa-outline-none" role="menu" aria-orientation="vertical" aria-labelledby="option-menu">

                <div class="elsa-py-1" role="none">
                  <a href="#" onClick={e => this.onExportClick(e)} class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900" role="menuitem">
                    {t('Export')}
                  </a>

                  <a href="#" onClick={e => this.onImportClick(e)} class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900" role="menuitem">
                    {t('Import')}
                  </a>

                  <a href="#" onClick={e => this.onDeleteClick(e)} class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-text-red-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900" role="menuitem">
                    {t('Delete')}
                  </a>
                </div>
              </div>
            </div>
          </span>
        </span>
        <input type="file" class="hidden" onChange={e => this.onFileInputChange(e)} ref={el => this.fileInput = el}/>
      </Host>
    );
  }

  renderMainButton = () => {
    const workflowDefinition = this.workflowDefinition;
    const isPublished = workflowDefinition.isPublished;

    if (isPublished)
      return this.publishing ? this.renderUnpublishingButton() : this.renderUnpublishButton();
    else
      return this.publishing ? this.renderPublishingButton() : this.renderPublishButton();
  }

  renderPublishButton = () => {
    const workflowDefinition = this.workflowDefinition;
    const isLatest = workflowDefinition.isLatest;
    const version = workflowDefinition.version;

    if (isLatest)
      return this.renderButton('Publish', this.onPublishClick);

    return this.renderButton(`Revert version ${version}`, this.onRevertClick);
  }

  renderPublishingButton() {
    const workflowDefinition = this.workflowDefinition;
    const isLatest = workflowDefinition.isLatest;
    const version = workflowDefinition.version;
    const text = isLatest ? 'Publishing' : `Publishing version ${version}`;

    return this.renderLoadingButton(text);
  }

  renderUnpublishButton = () => {
    return this.renderButton('Unpublish', e => this.onUnPublishClick(e));
  };

  renderPublishMenuItem = () => {
    return this.renderMenuItem('Publish', this.onPublishClick);
  };

  renderUnpublishMenuItem = () => {
    return this.renderMenuItem('Unpublish', this.onUnPublishClick);
  };

  renderMenuItem = (text: string, handler: (e: Event) => any) => {
    if (!this.workflowDefinition.isPublished)
      return undefined;

    const t = this.t;

    return (
      <div class="elsa-py-1" role="none">
        <a href="#" onClick={e => handler(e)} class="elsa-block elsa-px-4 elsa-py-2 elsa-text-sm elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900" role="menuitem">
          {t(text)}
        </a>
      </div>
    );
  };

  renderUnpublishingButton = () => {
    return this.renderLoadingButton('Unpublishing');
  };

  renderButton = (text: string, handler: (e: Event) => any) => {
    const t = this.t;

    return (
      <button type="button"
              onClick={e => handler(e)}
              class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-rounded-l-md elsa-border elsa-border-gray-300 elsa-bg-white elsa-text-sm elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-z-10 focus:elsa-outline-none focus:elsa-ring-1 focus:elsa-ring-blue-500 focus:elsa-border-blue-500">
        {text}
      </button>);
  }


  renderLoadingButton(text: string) {
    const t = this.t;

    return (
      <button type="button"
              disabled={true}
              class="elsa-relative elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-rounded-l-md elsa-border elsa-border-gray-300 elsa-bg-white elsa-text-sm elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-z-10 focus:elsa-outline-none focus:elsa-ring-1 focus:elsa-ring-blue-500 focus:elsa-border-blue-500">

        <svg class="elsa-animate-spin -elsa-ml-1 elsa-mr-3 elsa-h-5 elsa-w-5 elsa-text-blue-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="elsa-opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="elsa-opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
        </svg>
        {t(text)}
      </button>);
  }
}

Tunnel.injectProps(ElsaWorkflowPublishButton, ['serverUrl']);
