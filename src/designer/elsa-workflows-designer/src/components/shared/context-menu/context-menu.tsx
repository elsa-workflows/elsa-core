import {Component, h, Listen, Method, Prop} from '@stencil/core';
import {leave, toggle, enter} from 'el-transition'
import {ContextMenuAnchorPoint, MenuItem} from "./models";
import {TickIcon} from "../../icons/tooling/tick";

@Component({
  tag: 'elsa-context-menu',
  shadow: false,
})
export class ContextMenu {
  @Prop({mutable: true}) menuItems: Array<MenuItem> = [];
  @Prop() hideButton: boolean;
  @Prop() anchorPoint: ContextMenuAnchorPoint;

  contextMenu: HTMLElement;
  element: HTMLElement;

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

  private async onMenuItemClick(e: MouseEvent, menuItem: MenuItem) {
    e.preventDefault();

    if (!!menuItem.clickHandler)
      menuItem.clickHandler(e);

    if (menuItem.isToggle) {
      menuItem.checked = !menuItem.checked;
      this.menuItems = [...this.menuItems]; // Trigger a re-render.
    }

    this.closeContextMenu();
  }

  private getAnchorPointClass = (): string => {
    switch (this.anchorPoint) {
      case ContextMenuAnchorPoint.BottomLeft:
        return 'origin-bottom-left';
      case ContextMenuAnchorPoint.BottomRight:
        return 'origin-bottom-right';
      case ContextMenuAnchorPoint.TopLeft:
        return 'origin-top-left';
      case ContextMenuAnchorPoint.TopRight:
      default:
        return 'origin-top-right'
    }
  };

  render() {
    const anchorPointClass = this.getAnchorPointClass();
    const menuItems = this.menuItems;
    const hasAnyIcons = menuItems.find(x => !!x.icon) != null;

    return (
      <div class="relative flex justify-end items-center" ref={el => this.element = el}>
        {this.renderButton()}
        <div ref={el => this.contextMenu = el}
             data-transition-enter="transition ease-out duration-100"
             data-transition-enter-start="transform opacity-0 scale-95"
             data-transition-leave="transition ease-in duration-75"
             data-transition-leave-start="transform opacity-100 scale-100"
             data-transition-leave-end="transform opacity-0 scale-95"
             class={`hidden z-10 mx-3 absolute ${anchorPointClass} w-48 mt-1 rounded-md shadow-lg`}>
          <div class="rounded-md bg-white shadow-xs" role="menu" aria-orientation="vertical" aria-labelledby="project-options-menu-0">
            {menuItems.map(menuItem => {
              const anchorUrl = menuItem.anchorUrl || '#';
              const isToggle = menuItem.isToggle;
              const checked = menuItem.checked;
              return (
                <div class="py-1">
                  <a href={anchorUrl}
                     onClick={e => this.onMenuItemClick(e, menuItem)}
                     class="group flex items-center px-4 py-2 text-sm leading-5 text-gray-700 hover:bg-gray-100 hover:text-gray-900 focus:outline-none focus:bg-gray-100 focus:text-gray-900"
                     role="menuitem">
                    {menuItem.icon ? <span class="mr-3">{menuItem.icon}</span> : hasAnyIcons ? <span class="mr-7"/> : undefined}
                    <span class="flex-grow">{menuItem.text}</span>
                    {isToggle && checked ? <span class="float-right"><TickIcon/></span> : undefined}
                  </a>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    );
  }

  renderButton = () => {
    if (this.hideButton)
      return;

    return <button onClick={() => this.toggleMenu()} aria-has-popup="true" type="button"
                   class="w-8 h-8 inline-flex items-center justify-center text-gray-400 rounded-full bg-transparent hover:text-gray-500 focus:outline-none focus:text-gray-500 focus:bg-gray-100 transition ease-in-out duration-150">
      <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
        <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z"/>
      </svg>
    </button>;
  }
}
