import {Component, Event, EventEmitter, Host, h, Listen, Prop, State} from '@stencil/core';
import {leave, toggle} from 'el-transition';

@Component({
  tag: 'elsa-workflow-toolbar-menu',
  shadow: false,
})
export class WorkflowToolbarMenu {
  private menu: HTMLElement;
  private element: HTMLElement;
  private workflowBrowser: HTMLElsaWorkflowBrowserElement;
  private workflowInstanceBrowser: HTMLElsaWorkflowInstanceBrowserElement;

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeMenu();
  }

  private closeMenu = () => {
    leave(this.menu);
  };

  private toggleMenu = () => {
    toggle(this.menu);
  };

  private onWorkflowDefinitionsClick = async (e: MouseEvent) => {
    e.preventDefault();
    await this.workflowBrowser.show();
    this.closeMenu();
  };

  private onWorkflowInstancesClick = async (e: MouseEvent) => {
    e.preventDefault();
    await this.workflowInstanceBrowser.show();
    this.closeMenu();
  };

  render() {

    return (
      <Host class="block" ref={el => this.element = el}>
        <div class="ml-3 relative">
          <div>
            <button onClick={() => this.toggleMenu()}
                    type="button"
                    class="bg-gray-800 flex text-sm rounded-full focus:outline-none focus:ring-1 focus:ring-offset-1 focus:ring-gray-600"
                    aria-expanded="false" aria-haspopup="true">
              <span class="sr-only">Open user menu</span>
              <svg class="h-8 w-8 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z"/>
              </svg>
            </button>
          </div>

          <div ref={el => this.menu = el}
               data-transition-enter="transition ease-out duration-100"
               data-transition-enter-start="transform opacity-0 scale-95"
               data-transition-enter-end="transform opacity-100 scale-100"
               data-transition-leave="transition ease-in duration-75"
               data-transition-leave-start="transform opacity-100 scale-100"
               data-transition-leave-end="transform opacity-0 scale-95"
               class="hidden origin-top-right absolute right-0 mt-2 w-48 rounded-md shadow-lg py-1 bg-white ring-1 ring-black ring-opacity-5 focus:outline-none"
               role="menu" aria-orientation="vertical" aria-labelledby="user-menu-button" tabindex="-1">
            <a onClick={e => this.onWorkflowDefinitionsClick(e)} href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem" tabindex="-1" id="user-menu-item-0">Workflow Definitions</a>
            <a onClick={e => this.onWorkflowInstancesClick(e)} href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem" tabindex="-1" id="user-menu-item-1">Workflow Instances</a>
            <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem" tabindex="-1" id="user-menu-item-2">Settings</a>
          </div>
        </div>
        <elsa-workflow-browser ref={el => this.workflowBrowser = el}/>
        <elsa-workflow-instance-browser ref={el => this.workflowInstanceBrowser = el}/>
      </Host>
    );
  }
}
