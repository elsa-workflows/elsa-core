import {Component, Event, EventEmitter, Host, h, Listen, Prop} from '@stencil/core';
import {leave, toggle} from 'el-transition'

export interface PublishClickedArgs {
  begin: () => void;
  complete: () => void;
}

@Component({
  tag: 'elsa-workflow-publish-button',
  shadow: false,
})
export class WorkflowPublishButton {

  @Prop() publishing: boolean;
  @Event({bubbles: true}) newClicked: EventEmitter;
  @Event({bubbles: true}) publishClicked: EventEmitter<PublishClickedArgs>;
  @Event({bubbles: true}) unPublishClicked: EventEmitter;
  @Event({bubbles: true}) exportClicked: EventEmitter;
  @Event({bubbles: true}) importClicked: EventEmitter<File>;

  menu: HTMLElement;
  fileInput: HTMLInputElement;
  element: HTMLElement;

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeMenu();
  }

  private closeMenu() {
    leave(this.menu);
  }

  private toggleMenu() {
    toggle(this.menu);
  }

  private onPublishClick() {
    this.publishClicked.emit({
      begin: () => this.publishing = true,
      complete: () => this.publishing = false
    });

    leave(this.menu);
  }

  private onUnPublishClick(e: Event) {
    e.preventDefault();
    this.unPublishClicked.emit();
    leave(this.menu);
  }

  private onNewClick(e: Event) {
    e.preventDefault();
    this.newClicked.emit();
    leave(this.menu);
  }

  private async onExportClick(e: Event) {
    e.preventDefault();
    this.exportClicked.emit();
    leave(this.menu);
  }

  private async onImportClick(e: Event) {
    e.preventDefault();
    this.fileInput.value = null;
    this.fileInput.click();

    leave(this.menu);
  }

  private async onFileInputChange(e: Event) {
    const files = this.fileInput.files;

    if (files.length == 0) {
      return;
    }

    this.importClicked.emit(files[0]);
  }

  render() {
    return (
      <Host class="block" ref={el => this.element = el}>
        <span class="relative z-0 inline-flex shadow-sm rounded-md">
          {this.publishing ? this.renderPublishingButton() : this.renderPublishButton()}
          <span class="-ml-px relative block">
            <button onClick={() => this.toggleMenu()} id="option-menu" type="button"
                    class="relative inline-flex items-center px-2 py-2 rounded-r-md border border-blue-600 bg-blue-600 text-sm font-medium text-white hover:bg-blue-700 focus:z-10 focus:outline-none hover:border-blue-700">
              <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"/>
              </svg>
            </button>
            <div ref={el => this.menu = el}
                 data-transition-enter="transition ease-out duration-100"
                 data-transition-enter-start="transform opacity-0 scale-95"
                 data-transition-enter-end="transform opacity-100 scale-100"
                 data-transition-leave="transition ease-in duration-75"
                 data-transition-leave-start="transform opacity-100 scale-100"
                 data-transition-leave-end="transform opacity-0 scale-95"
                 class="hidden origin-bottom-right absolute right-0 top-10 mb-2 -mr-1 w-56 rounded-md shadow-lg bg-white ring-1 ring-black ring-opacity-5">

              <div class="divide-y divide-gray-100 focus:outline-none" role="menu" aria-orientation="vertical" aria-labelledby="option-menu">

              <div class="py-1" role="none">
                  <a href="#" onClick={e => this.onNewClick(e)}
                     class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900"
                     role="menuitem">
                    New
                  </a>
                </div>

                <div class="py-1" role="none">
                  <a href="#" onClick={e => this.onExportClick(e)}
                     class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900"
                     role="menuitem">
                    Export
                  </a>

                  <a href="#" onClick={e => this.onImportClick(e)}
                     class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900"
                     role="menuitem">
                    Import
                  </a>
                </div>

                {this.renderUnpublishButton()}

              </div>
            </div>
          </span>
        </span>
        <input type="file" class="hidden" onChange={e => this.onFileInputChange(e)} ref={el => this.fileInput = el}/>
      </Host>
    );
  }

  private renderPublishButton() {

    return (
      <button type="button"
              onClick={() => this.onPublishClick()}
              class="relative inline-flex items-center px-4 py-2 rounded-l-md border border-blue-600 bg-blue-600 text-sm font-medium text-white hover:bg-blue-700 focus:z-10 focus:outline-none focus:ring-blue-600 hover:border-blue-700">

        Publish
      </button>);
  }

  private renderPublishingButton() {

    return (
      <button type="button"
              disabled={true}
              class="relative inline-flex items-center px-4 py-2 rounded-l-md border border-blue-600 bg-blue-600 text-sm font-medium text-white hover:bg-blue-700 focus:z-10 focus:outline-none focus:ring-blue-600 hover:border-blue-700">

        <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-blue-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
        </svg>
        Publishing
      </button>);
  }

  private renderUnpublishButton() {

    return (
      <div class="py-1" role="none">
        <a href="#" onClick={e => this.onUnPublishClick(e)}
           class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900"
           role="menuitem">
          Unpublish
        </a>
      </div>
    );
  }
}
