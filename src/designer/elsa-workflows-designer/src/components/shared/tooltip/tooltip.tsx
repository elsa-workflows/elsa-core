import {Component, h, Prop} from '@stencil/core';

@Component({
  tag: 'elsa-tooltip',
  styleUrl: 'tooltip.scss',
})
export class Tooltip {
  @Prop() tooltipContent: any;
  @Prop() tooltipPosition?: string = 'right';

  getTooltipPositionClasses(tooltipPosition: string) {
    const tooltipPositionClasses = {
      right: 'top-1/2 -translate-y-1/2 left-full ml-4',
      left: 'top-1/2 -translate-y-1/2 right-full mr-4',
      top: 'left-1/2 -translate-x-1/2 bottom-full mb-4',
      bottom: 'left-1/2 -translate-x-1/2 top-full mt-4',
    };

    const triangleClasses = {
      right: 'left-0 -ml-2 bottom-0 top-0 h-full',
      left: 'right-0 -mr-2 bottom-0 top-0 h-full rotate-180',
      top: 'bottom-0 left-1/2 -mt-1 top-full -translate-x-1/2 -rotate-90',
      bottom: 'top-0 left-1/2 -mb-1 bottom-full -translate-x-1/2 h-full rotate-90',
    };

    return {
      tooltipClass: tooltipPositionClasses[tooltipPosition],
      triangleClass: triangleClasses[tooltipPosition],
    };
  }

  render() {
    const {tooltipClass, triangleClass} = this.getTooltipPositionClasses(this.tooltipPosition);

    return (
      <div class="relative tooltip-container w-full">
        <slot/>
        {this.tooltipContent && (
          <div role="tooltip" class={`tooltip ${tooltipClass} z-20 w-48 absolute transition duration-150 ease-in-out shadow-lg bg-gray-800 text-white p-4 rounded`}>
            <svg class={`absolute ${triangleClass}`} width="9px" height="16px" viewBox="0 0 9 16" version="1.1" xmlns="http://www.w3.org/2000/svg">
              <g stroke="none" stroke-width="1" fill="none" fill-rule="evenodd">
                <g id="Tooltips-" transform="translate(-874.000000, -1029.000000)" fill="#111827">
                  <g transform="translate(850.000000, 975.000000)">
                    <g transform="translate(24.000000, 0.000000)">
                      <polygon
                        transform="translate(4.500000, 62.000000) rotate(-90.000000) translate(-4.500000, -62.000000) "
                        points="4.5 57.5 12.5 66.5 -3.5 66.5"
                      />
                    </g>
                  </g>
                </g>
              </g>
            </svg>
            <p class="text-white text-sm"> {this.tooltipContent}</p>
          </div>
        )}
      </div>
    );
  }
}
