import {Component, h} from '@stencil/core';

@Component({
  tag: 'elsa-workflow-toolbar'
})
export class WorkflowToolbar {

  render() {
    return <nav class="bg-gray-800">
      <div class="mx-auto px-2 sm:px-6 lg:px-6">
        <div class="relative flex items-center justify-end h-16">

          <div class="absolute inset-y-0 right-0 flex items-center pr-2 sm:static sm:inset-auto sm:ml-6 sm:pr-0 z-20">

            {/* Notifications*/}
            <button type="button" class="bg-gray-800 p-1 rounded-full text-gray-400 hover:text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-gray-800 focus:ring-white mr-4">
              <span class="sr-only">View notifications</span>
              <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
              </svg>
            </button>

            {/* Publish */}
            <div class="flex-shrink-0">
              <elsa-workflow-publish-button/>
            </div>

            {/* Menu */}
            <elsa-workflow-toolbar-menu/>
          </div>
        </div>
      </div>
    </nav>;
  }
}
