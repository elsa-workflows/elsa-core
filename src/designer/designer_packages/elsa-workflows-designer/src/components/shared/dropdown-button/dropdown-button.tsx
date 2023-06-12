import {Component, Event, EventEmitter, Listen, h, Prop, Host} from '@stencil/core';
import {leave, toggle} from 'el-transition'
import {groupBy, sortBy, map} from 'lodash';
import {DropdownButtonItem, DropdownButtonOrigin} from "./models";

@Component({
  tag: 'elsa-dropdown-button',
  shadow: false,
})
export class DropdownButton {
  @Prop() public text: string;
  @Prop() public icon?: any;
  @Prop() public handler?: () => void;
  @Prop() public origin: DropdownButtonOrigin = DropdownButtonOrigin.TopLeft;
  @Prop() public items: Array<DropdownButtonItem> = [];
  @Prop() public theme: ('Primary' | 'Secondary') = 'Primary';
  @Prop() public disabled: boolean;
  @Event() public itemSelected: EventEmitter<DropdownButtonItem>
  @Event() public menuOpened: EventEmitter<void>

  private contextMenu: HTMLElement;
  private element: HTMLElement;

  public render() {
    const disabled = this.disabled;
    const buttonClass = this.theme == 'Secondary' ? 'tw-border-gray-300 tw-bg-white tw-text-gray-700 hover:tw-bg-gray-50 focus:tw-ring-blue-500 hover:tw-border-blue-500' + (disabled==true ? ' tw-opacity-50' : '') : 'tw-border-blue-600 tw-bg-blue-600 tw-text-white hover:tw-bg-blue-700 focus:tw-ring-blue-600 hover:tw-border-blue-700' + (disabled==true ? ' tw-opacity-50' : '');
    const arrowClass = this.theme == 'Secondary' ? 'tw-border-gray-300 tw-bg-white tw-text-gray-700 hover:tw-bg-gray-50 hover:tw-border-blue-500' + (disabled==true ? ' tw-opacity-50' : '') : 'tw-border-blue-600 tw-bg-blue-600 tw-text-white hover:tw-bg-blue-700 hover:tw-border-blue-700' + (disabled==true ? ' tw-opacity-50' : '');
    const handler = this.handler ?? (() => this.toggleMenu());

    return (
      <Host class="tw-block" ref={el => this.element = el}>
        <span class="tw-relative tw-z-0 tw-inline-flex tw-shadow-sm tw-rounded-md">
          <button
            type="button"
            disabled={disabled}
                  class={`tw-relative tw-inline-flex tw-items-center tw-px-4 tw-py-2 tw-rounded-l-md tw-border tw-text-sm tw-font-medium focus:tw-z-10 focus:tw-outline-none ${buttonClass}`}
                  onClick={handler}>
            {this.renderIcon()}
            {this.text}
          </button>
          <div class="-tw-ml-px tw-block">
            <button type="button"
              disabled={disabled}
                    class={`tw-relative tw-inline-flex tw-items-center tw-px-2 tw-py-2 tw-rounded-r-md tw-border tw-text-sm tw-font-medium focus:tw-z-10 focus:tw-outline-none ${arrowClass}`}
                    onClick={() => this.toggleMenu()}
                    aria-expanded="true" aria-haspopup="true">
              <svg class="tw-h-5 tw-w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/>
              </svg>
            </button>
            {this.renderMenu()}
          </div>
        </span>
      </Host>
    );
  }

  private renderMenu() {
    if (this.items.length == 0)
      return;

    const originClass = this.getOriginClass();

    return <div ref={el => this.contextMenu = el}
                data-transition-enter="tw-transition tw-ease-out tw-duration-100"
                data-transition-enter-start="tw-transform tw-opacity-0 tw-scale-95"
                data-transition-leave="tw-transition tw-ease-in tw-duration-75"
                data-transition-leave-start="tw-transform tw-opacity-100 tw-scale-100"
                data-transition-leave-end="tw-transform tw-opacity-0 tw-scale-95"
                class={`hidden ${originClass} tw-z-10 tw-absolute tw-mt-2 tw-w-56 tw-rounded-md tw-shadow-lg tw-bg-white tw-ring-1 tw-ring-black tw-ring-opacity-5`}>
      <div class="tw-py-1" role="menu" aria-orientation="vertical">
        {this.renderItems()}
      </div>
    </div>;
  }

  private renderItems() {

    const groups = groupBy(this.items, x => x.group ?? 0);

    return <div class="tw-divide-y tw-divide-gray-100 focus:tw-outline-none" role="menu" aria-orientation="vertical" aria-labelledby="option-menu">
      {map(groups, menuItemGroup => {

        return <div class="tw-py-1" role="none">
          {menuItemGroup.map(menuItem => {

            const selectedCssClass = menuItem.isSelected ? 'tw-bg-blue-600 hover:tw-bg-blue-700 tw-text-white' : 'hover:tw-bg-gray-100 tw-text-gray-700 hover:tw-text-gray-900';
            return (
              <div class="tw-py-1" role="none">
                <a href="#" onClick={e => this.onItemClick(e, menuItem)}
                   class={`tw-block tw-px-4 tw-py-2 tw-text-sm ${selectedCssClass}`}
                   role="menuitem">
                  {menuItem.text}
                </a>
              </div>);
          })}

        </div>
      })}
    </div>
  }

  private renderIcon = () => this.icon ? this.icon : undefined;

  private closeContextMenu() {
    if (!!this.contextMenu)
      leave(this.contextMenu);
  }

  private toggleMenu() {
    if (!!this.contextMenu) {
      toggle(this.contextMenu);
      this.menuOpened.emit();
    }
  }

  private getOriginClass(): string {
    switch (this.origin) {
      case DropdownButtonOrigin.TopLeft:
        return 'tw-left-0 tw-origin-top-left';
      case DropdownButtonOrigin.TopRight:
      default:
        return 'tw-right-0 tw-origin-top-right';
    }
  }

  private async onItemClick(e: Event, menuItem: DropdownButtonItem) {
    e.preventDefault();

    if (!!menuItem.handler)
      menuItem.handler();

    this.itemSelected.emit(menuItem);
    this.closeContextMenu();
  }

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeContextMenu();
  }
}
