import {Component, h, Listen, Method, Prop, Element} from '@stencil/core';
import {leave, toggle, enter} from 'el-transition';
import {groupBy, map} from 'lodash';
import {ContextMenuAnchorPoint, ContextMenuItem} from "./models";
import {TickIcon} from "../../icons/tooling/tick";

@Component({
  tag: 'elsa-context-menu',
  shadow: false,
})
export class ContextMenu {
  @Prop({mutable: true}) menuItems: Array<ContextMenuItem> = [];
  @Prop() hideButton: boolean;
  @Prop() anchorPoint: ContextMenuAnchorPoint;

  contextMenu: HTMLElement;
  @Element() element: HTMLElement;

  @Method()
  async open(): Promise<void> {
    this.showContextMenu();
  }

  @Method()
  async close(): Promise<void> {
    this.closeContextMenu();
  }

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeContextMenu();
  }

  private showContextMenu() {
    if (!!this.contextMenu)
      enter(this.contextMenu);
  }

  private closeContextMenu() {
    if (!!this.contextMenu)
      leave(this.contextMenu);
  }

  private toggleMenu() {
    toggle(this.contextMenu);
  }

  private async onMenuItemClick(e: MouseEvent, menuItem: ContextMenuItem) {
    e.preventDefault();

    if (!!menuItem.handler)
      menuItem.handler(e);

    if (menuItem.isToggle) {
      menuItem.checked = !menuItem.checked;
      this.menuItems = [...this.menuItems]; // Trigger a re-render.
    }

    this.closeContextMenu();
  }

  private getAnchorPointClass = (): string => {
    switch (this.anchorPoint) {
      case ContextMenuAnchorPoint.BottomLeft:
        return 'tw-origin-bottom-left tw-left-0';
      case ContextMenuAnchorPoint.BottomRight:
        return 'tw-origin-bottom-right tw-right-0';
      case ContextMenuAnchorPoint.TopLeft:
        return 'tw-origin-top-left tw-left-0';
      case ContextMenuAnchorPoint.TopRight:
      default:
        return 'tw-origin-top-right tw-right-0'
    }
  };

  render() {
    const anchorPointClass = this.getAnchorPointClass();
    const menuItems = this.menuItems;
    const menuItemGroups:any = groupBy(menuItems, x => x.group ?? 0);

    return (
      <div class="tw-relative tw-flex tw-justify-end tw-items-center">
        {this.renderButton()}
        <div ref={el => this.contextMenu = el}
             data-transition-enter="tw-transition tw-ease-out tw-duration-100"
             data-transition-enter-start="tw-transform tw-opacity-0 tw-scale-95"
             data-transition-leave="tw-transition tw-ease-in tw-duration-75"
             data-transition-leave-start="tw-transform tw-opacity-100 tw-scale-100"
             data-transition-leave-end="tw-transform tw-opacity-0 tw-scale-95"
             class={`hidden tw-z-10 tw-mx-3 tw-absolute ${anchorPointClass} tw-w-48 tw-mt-1 tw-rounded-md tw-shadow-lg`}>
          <div class="tw-rounded-md tw-bg-white tw-shadow-xs tw-ring-1 tw-ring-black tw-ring-opacity-5 tw-divide-y tw-divide-gray-100 focus:tw-outline-none" role="menu" aria-orientation="vertical" aria-labelledby="project-options-menu-0">
            {this.renderMenuItemGroups(menuItemGroups)}
          </div>
        </div>
      </div>
    );
  }

  renderMenuItemGroups = (menuItemGroups: Array<any>) => {
    if (Object.keys(menuItemGroups).length == 0)
      return;

    return map(menuItemGroups, group => this.renderMenuItems(group));
  }

  renderMenuItems = (menuItems: Array<ContextMenuItem>) => {
    if (menuItems.length == 0)
      return;

    const hasAnyIcons = menuItems.find(x => !!x.icon) != null;

    return <div class="tw-py-1">
      {menuItems.map(menuItem => {
        const anchorUrl = menuItem.anchorUrl || '#';
        const isToggle = menuItem.isToggle;
        const checked = menuItem.checked;
        return (

          <a href={anchorUrl}
             onClick={e => this.onMenuItemClick(e, menuItem)}
             class="tw-group tw-flex tw-items-center tw-px-4 tw-py-2 tw-text-sm tw-leading-5 tw-text-gray-700 hover:tw-bg-gray-100 hover:tw-text-gray-900 focus:tw-outline-none focus:tw-bg-gray-100 focus:tw-text-gray-900"
             role="menuitem">
            {menuItem.icon ? <span class="tw-mr-3">{menuItem.icon}</span> : hasAnyIcons ? <span class="tw-mr-7"/> : undefined}
            <span class="tw-flex-grow">{menuItem.text}</span>
            {isToggle && checked ? <span class="tw-float-right"><TickIcon/></span> : undefined}
          </a>
        );
      })}
    </div>
  };

  renderButton = () => {
    if (this.hideButton)
      return;

    return <button onClick={() => this.toggleMenu()} aria-has-popup="true" type="button"
                   class="tw-w-8 tw-h-8 tw-inline-flex tw-items-center tw-justify-center tw-text-gray-400 tw-rounded-full tw-bg-transparent hover:tw-text-gray-500 focus:tw-outline-none focus:tw-text-gray-500 focus:tw-bg-gray-100 tw-transition tw-ease-in-out tw-duration-150">
      <svg class="tw-w-5 tw-h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
        <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z"/>
      </svg>
    </button>;
  }
}
