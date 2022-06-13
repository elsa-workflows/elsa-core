import { Component, h } from '@stencil/core';

@Component({
  tag: 'elsa-workflow-definition-editor-toolbar',
})
export class Toolbar {
  render() {
    return (
      <div class="flex justify-center absolute border-b border-gray-200 top-0 px-1 pl-4 py-2 text-sm bg-white z-10 elsa-panel-toolbar">
        <elsa-tooltip tooltipPosition="right" tooltipContent={<p class="text-gray-600 text-sm">Currently in development</p>}>
          <button class="relative inline-flex items-center px-4 py-2 rounded border border-blue-600 bg-blue-600 text-sm font-medium text-white hover:bg-blue-700 focus:z-10 focus:outline-none focus:ring-blue-600 hover:border-blue-700">
            Auto-Layout
          </button>
        </elsa-tooltip>
      </div>
    );
  }
}
