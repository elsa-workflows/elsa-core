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
      <Host class="tw-block" ref={el => this.element = el}>
        <div class="elsa-toolbar-menu-wrapper tw-relative">
          <div>
            <button onClick={() => this.toggleMenu()}
                    type="button"
                    class="tw-bg-gray-800 tw-flex tw-text-sm tw-rounded-full focus:tw-outline-none focus:tw-ring-1 focus:tw-ring-offset-1 focus:tw-ring-gray-600"
                    aria-expanded="false" aria-haspopup="true">
              <span class="tw-sr-only">Open user menu</span>
              <svg class="tw-h-8 tw-w-8 tw-text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z"/>
              </svg>
            </button>
          </div>

          <div ref={el => this.menu = el}
               data-transition-enter="tw-transition tw-ease-out tw-duration-100"
               data-transition-enter-start="tw-transform tw-opacity-0 tw-scale-95"
               data-transition-enter-end="tw-transform tw-opacity-100 tw-scale-100"
               data-transition-leave="tw-transition tw-ease-in tw-duration-75"
               data-transition-leave-start="tw-transform tw-opacity-100 tw-scale-100"
               data-transition-leave-end="tw-transform tw-opacity-0 tw-scale-95"
               class="hidden tw-origin-top-right tw-absolute tw-right-0 tw-mt-2 tw-w-48 tw-rounded-md tw-shadow-lg tw-py-1 tw-bg-white tw-ring-1 tw-ring-black tw-ring-opacity-5 focus:tw-outline-none"
               role="menu" aria-orientation="vertical" aria-labelledby="user-menu-button" tabindex="-1">
            {menuItems.map(menuItem => <a onClick={e => this.onMenuItemClick(e, menuItem)} href="#" role="menuitem" tabindex="-1">{menuItem.text}</a>)}
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
