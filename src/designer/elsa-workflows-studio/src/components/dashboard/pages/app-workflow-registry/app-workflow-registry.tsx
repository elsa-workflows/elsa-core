import {Component, h} from '@stencil/core';

@Component({
  tag: 'app-workflow-registry',
  styleUrl: 'app-workflow-registry.css',
  shadow: false,
})
export class AppHome {
  render() {
    return (
      <div>
        <div class="border-b border-gray-200 px-4 py-4 sm:flex sm:items-center sm:justify-between sm:px-6 lg:px-8 bg-white">
          <div class="flex-1 min-w-0">
            <h1 class="text-lg font-medium leading-6 text-gray-900 sm:truncate">
              Workflow Registry
            </h1>
          </div>
          <div class="mt-4 flex sm:mt-0 sm:ml-4">
            <stencil-route-link url="/workflow-definitions/new" class="order-0 inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:order-1 sm:ml-3">
              Create Workflow
            </stencil-route-link>
          </div>
        </div>

        <div class="align-middle inline-block min-w-full border-b border-gray-200">
          <table class="min-w-full">
            <thead>
            <tr class="border-t border-gray-200">
              <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider"><span class="lg:pl-2">Name</span></th>
              <th class="px-6 py-3 border-b border-gray-200 bg-gray-50 text-left text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                Instances
              </th>
              <th class="hidden md:table-cell px-6 py-3 border-b border-gray-200 bg-gray-50 text-right text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                Latest Version
              </th>
              <th class="hidden md:table-cell px-6 py-3 border-b border-gray-200 bg-gray-50 text-right text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider">
                Published Version
              </th>
              <th class="pr-6 py-3 border-b border-gray-200 bg-gray-50 text-right text-xs leading-4 font-medium text-gray-500 uppercase tracking-wider"/>
            </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-100">
            <tr>
              <td class="px-6 py-3 whitespace-no-wrap text-sm leading-5 font-medium text-gray-900">
                <div class="flex items-center space-x-3 lg:pl-2"><a href="/workflow-registry/7eed1333c2674370a1e9b300aa94fda2/viewer" class="truncate hover:text-gray-600"><span>Customer Call Flow</span></a></div>
              </td>

              <td class="px-6 py-3 text-sm leading-5 text-gray-500 font-medium">
                <div class="flex items-center space-x-2">
                  <div class="flex items-center space-x-2">
                    <div class="flex flex-shrink-0 -space-x-1"><a class="max-w-none h-9 w-9 rounded-full text-white shadow-solid p-2 text-xs bg-blue-500 hover:bg-blue-400" href="#">4</a>
                      <a class="max-w-none h-9 w-9 rounded-full text-white shadow-solid p-2 text-xs bg-green-500 hover:bg-green-400" href="#">999</a>
                      <a class="max-w-none h-9 w-9 rounded-full text-white shadow-solid p-2 text-xs bg-red-500 hover:bg-red-400" href="#">75</a></div>
                  </div>
                </div>
              </td>
              <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-right">47</td>
              <td class="hidden md:table-cell px-6 py-3 whitespace-no-wrap text-sm leading-5 text-gray-500 text-right">47</td>
              <td class="pr-6">
                <elsa-context-menu/>
              </td>
            </tr>
            </tbody>
          </table>
        </div>
      </div>
    );
  }
}
