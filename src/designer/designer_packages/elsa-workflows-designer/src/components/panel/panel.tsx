import { Component, Event, EventEmitter, h, Host, Prop, State } from '@stencil/core';
import { PanelPosition, PanelStateChangedArgs } from './models';
import { applyResize } from './resize';

@Component({
  tag: 'elsa-panel',
  styleUrl: 'panel.scss',
})
export class Panel {
  @Prop() position: PanelPosition = PanelPosition.Left;
  @Event() expandedStateChanged: EventEmitter<PanelStateChangedArgs>;
  @State() isExpanded: boolean = true;
  dragging = false;

  private onToggleClick = () => {
    if (this.isExpanded) {
      applyResize({ position: this.position, isHide: true });
    } else {
      applyResize({ position: this.position, isDefault: true });
    }
    this.isExpanded = !this.isExpanded;
    this.expandedStateChanged.emit({ expanded: this.isExpanded });
  };

  onBodyMouseUp = () => {
    if (this.dragging) {
      this.clearJSEvents();
    }
  };

  componentWillLoad() {
    document.addEventListener('mouseup', this.onBodyMouseUp);
    applyResize({ position: this.position, isDefault: true });
  }

  disconnectedCallback() {
    document.removeEventListener('mouseup', this.onBodyMouseUp);
  }

  clearJSEvents() {
    const body = document.body;
    this.dragging = false;
    body.removeEventListener('mousemove', this.resize);
  }

  resize = (e: MouseEvent) => {
    if (this.position === PanelPosition.Right) {
      applyResize({ position: PanelPosition.Right, size: document.body.clientWidth - e.pageX });
    }
    if (this.position === PanelPosition.Left) {
      applyResize({ position: PanelPosition.Left, size: e.pageX });
    }
    if (this.position === PanelPosition.Bottom) {
      applyResize({ position: PanelPosition.Bottom, size: document.body.offsetHeight - e.pageY });
    }
  };

  onDragBarMouseDown = (e: MouseEvent) => {
    e.preventDefault();
    const body = document.body;
    this.dragging = true;
    body.addEventListener('mousemove', this.resize);
  };

  render() {
    const isExpanded = this.isExpanded;
    const position = this.position;
    const stateClass = isExpanded ? 'panel-state-expanded' : 'panel-state-collapsed';

    const containerClassMap = [];
    containerClassMap[PanelPosition.Left] = 'panel-position-left tw-left-0 tw-top-0 tw-bottom-0 tw-border-r';
    containerClassMap[PanelPosition.Top] = 'panel-position-top tw-left-0 tw-top-0 tw-right-0 tw-border-b';
    containerClassMap[PanelPosition.Bottom] = 'panel-position-bottom h-0 tw-bottom-0 tw-border-t';
    containerClassMap[PanelPosition.Right] = 'panel-position-right tw-right-0 tw-top-0 tw-bottom-0 tw-border-l';
    const containerCssClass = containerClassMap[position];

    const toggleClassMap = {};
    toggleClassMap[PanelPosition.Left] = 'elsa-panel-toggle-left';
    toggleClassMap[PanelPosition.Top] = 'elsa-panel-toggle-top';
    toggleClassMap[PanelPosition.Right] = 'elsa-panel-toggle-right';
    toggleClassMap[PanelPosition.Bottom] = 'elsa-panel-toggle-bottom';

    const toggleCssClass = toggleClassMap[position];
    const iconOrientationCssClass = isExpanded ? 'tw-rotate-180': '';

    const dragBarClassMap = {
      [PanelPosition.Left]: 'tw-right-0 tw-h-full tw-cursor-col-resize tw-w-1',
      [PanelPosition.Right]: 'tw-left-0 tw-h-full tw-cursor-col-resize tw-w-1',
      [PanelPosition.Bottom]: 'tw-top-0 tw-w-full tw-cursor-row-resize tw-h-1',
    };

    const dragBarClass = dragBarClassMap[this.position];

    return (
      <Host class={`panel tw-absolute tw-bg-white tw-z-20 ${containerCssClass} ${stateClass}`}>
        <div class={`tw-absolute tw-opacity-0 tw-bg-blue-400 tw-transition tw-ease-in-out tw-duration-300 hover:tw-opacity-100 tw-z-10 ${dragBarClass}`} onMouseDown={this.onDragBarMouseDown} />
        <div class="panel-content-container">
          <slot />
        </div>

        <div class={`tw-text-white ${toggleCssClass}`} onClick={() => this.onToggleClick()}>
          <svg
            class={`tw-h-6 tw-w-6 tw-text-gray-700 ${iconOrientationCssClass}`}
            width="24"
            height="24"
            viewBox="0 0 24 24"
            stroke-width="2"
            stroke="currentColor"
            fill="none"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <path stroke="none" d="M0 0h24v24H0z" />
            <polyline points="9 6 15 12 9 18" />
          </svg>
        </div>
      </Host>
    );
  }
}
