import { Component, h, Prop, Element, State } from '@stencil/core';

@Component({
  tag: 'elsa-tooltip',
  styleUrl: 'tooltip.scss',
})
export class Tooltip {
  @Prop() tooltipContent: any;
  /** Specifying `auto` will default to `bottom`, but will reposition to `top` if the tooltip height runs offscreen  */
  @Prop() tooltipPosition?: 'right' | 'left' | 'top' | 'bottom' | 'auto' = 'right';
  @State() triangleClass: string = '';
  @Element() private element: HTMLElement;

  private getTooltipPositionClasses = (tooltipPosition: 'right' | 'left' | 'top' | 'bottom') => {
    const triangleClasses = {
      right: 'tw-left-0 -tw-ml-2 tw-bottom-0 tw-top-0 tw-h-full',
      left: 'tw-right-0 -tw-mr-2 tw-bottom-0 tw-top-0 tw-h-full tw-rotate-180',
      top: 'tw-left-1/2 -tw-mt-1 tw-top-full -tw-translate-x-1/2 -tw-rotate-90',
      bottom: 'tw-left-1/2 -tw-mb-1 tw-bottom-full -tw-translate-x-1/2 tw-rotate-90',
    };

    this.triangleClass = triangleClasses[tooltipPosition];
  };

  private onMouseOver = () => {
    const rootNode: HTMLElement = this.element.querySelector('.tooltip-container');
    const tooltip: HTMLElement = this.element.querySelector('.tooltip');

    if(!tooltip)
      return;

    const rect = rootNode.getBoundingClientRect();
    const tooltipRect = tooltip.getBoundingClientRect();

    let left = '0px';
    let top = '0px';
    let caretPaddingPx = 16;
    let tooltipPosition = this.tooltipPosition!;

    if (tooltipPosition === 'auto') {
      // Calculate what the Y would be for bottom
      const y = rect.y + rect.height + caretPaddingPx + tooltipRect.height;

      if (this.isOutOfBounds(y)) {
        // Use Top positioning
        tooltipPosition = 'top';
      } else {
        // Use Bottom positioning
        tooltipPosition = 'bottom';
      }
    }

    switch (tooltipPosition) {
      case 'right':
        left = `${rect.x + rect.width + caretPaddingPx}px`;
        top = `${(rect.height - tooltipRect.height) / 2 + rect.top}px`;
        this.getTooltipPositionClasses('right');
        break;
      case 'left':
        left = `${rect.left - tooltipRect.width - caretPaddingPx}px`;
        top = `${(rect.height - tooltipRect.height) / 2 + rect.top}px`;
        this.getTooltipPositionClasses('left');
        break;
      case 'top':
        left = `${(rect.width - tooltipRect.width) / 2 + rect.left}px`;
        top = `${rect.y - tooltipRect.height - caretPaddingPx}px`;
        this.getTooltipPositionClasses('top');
        break;
      case 'bottom':
        left = `${(rect.width - tooltipRect.width) / 2 + rect.left}px`;
        top = `${rect.y + rect.height + caretPaddingPx}px`;
        this.getTooltipPositionClasses('bottom');
        break;
    }

    tooltip.style.left = left;
    tooltip.style.top = top;
  };
  private isOutOfBounds = (y: number) => {
    const viewportHeight = window.innerHeight;

    // If any part of the tooltip is out of the viewport,
    // rather than entirely out of the viewport
    return y > viewportHeight;
  };

  render() {
    return (
      <div onMouseOver={() => this.onMouseOver()} class="tooltip-container tw-relative tw-w-full">
        <slot />
        {this.tooltipContent && (
          <div role="tooltip" class={`tooltip tw-z-20 tw-w-48 tw-fixed tw-transition tw-duration-150 tw-ease-in-out tw-shadow-lg tw-bg-gray-800 tw-text-white tw-p-4 tw-rounded`}>
            <svg class={`tw-absolute ${this.triangleClass}`} width="9px" height="16px" viewBox="0 0 9 16" version="1.1" xmlns="http://www.w3.org/2000/svg">
              <g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd">
                <g id="Tooltips-" tw-transform="translate(-874.000000, -1029.000000)" fill="#111827">
                  <g tw-transform="translate(850.000000, 975.000000)">
                    <g tw-transform="translate(24.000000, 0.000000)">
                      <polygon tw-transform="translate(4.500000, 62.000000) rotate(-90.000000) translate(-4.500000, -62.000000) " points="4.5 57.5 12.5 66.5 -3.5 66.5" />
                    </g>
                  </g>
                </g>
              </g>
            </svg>
            <p class="tw-text-white tw-text-sm"> {this.tooltipContent}</p>
          </div>
        )}
      </div>
    );
  }
}
