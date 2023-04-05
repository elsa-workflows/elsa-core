import {Component, Event, EventEmitter, Host, h, Listen, Prop, State} from '@stencil/core';
import {leave, toggle} from 'el-transition';
import {EventBus} from "../../../services";
import {Container} from "typedi";
import {ToolbarMenuItem} from "./models";
import toolbarButtonMenuItemStore from "../../../data/toolbar-button-menu-item-store";
import NotificationService from "../../../modules/notifications/notification-service";

@Component({
  tag: 'elsa-workflow-toolbar-menu',
  shadow: false,
})
export class WorkflowToolbarMenu {
  private readonly eventBus: EventBus;
  private menu: HTMLElement;
  private element: HTMLElement;
  private isMenuOpen = false;

  constructor() {
    this.eventBus = Container.get(EventBus);
  }

  private closeMenu = () => {
    leave(this.menu);
    this.isMenuOpen = false;
  };

  private toggleMenu = () => {
    toggle(this.menu);
    this.isMenuOpen = !this.isMenuOpen;
    if (this.isMenuOpen) {
      NotificationService.hideAllNotifications();
    }
  };

  render() {
    const menuItems: Array<ToolbarMenuItem> = toolbarButtonMenuItemStore.items;

    return (
      <Host class="block" ref={el => this.element = el}>
        <div class="toolbar-menu-wrapper relative">
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
            {menuItems.map(menuItem => <a onClick={e => this.onMenuItemClick(e, menuItem)} href="#" role="menuitem" tabindex="-1">{menuItem.text}</a>)}
            <a href="#" role="menuitem" tabindex="-1">Settings</a>
          </div>
        </div>
      </Host>
    );
  }

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeMenu();
  }

  private onMenuItemClick = async (e: MouseEvent, menuItem: ToolbarMenuItem) => {
    e.preventDefault();
    await menuItem.onClick();
    this.closeMenu();
  };
}
