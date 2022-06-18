import {Component, h} from "@stencil/core";
import {FlowchartIcon, IfIcon} from "../../icons/activities";

@Component({
  tag: 'elsa-workflow-navigator',
  shadow: false
})
export class WorkflowNavigator {
  render() {
    return <div class="ml-8">
      <nav class="flex" aria-label="Breadcrumb">
        <ol role="list" class="flex items-center space-x-4">
          <li>
            <div>
              <a href="#" class="block flex items-center text-gray-400 hover:text-gray-500">
                <div class="bg-blue-500 rounded">
                  <FlowchartIcon/>
                </div>
                <span class="ml-4 text-sm font-medium text-gray-500 hover:text-gray-700">Workflow 1</span>
              </a>
            </div>
          </li>

          <li>
            <div class="flex items-center">
              <svg class="flex-shrink-0 h-5 w-5 text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
              </svg>
              <div class="bg-blue-500 rounded">
                <IfIcon/>
              </div>
              <a href="#" class="ml-4 text-sm font-medium text-gray-500 hover:text-gray-700">If 1</a>
            </div>
          </li>

          <li>
            <div class="flex items-center">

              <svg class="flex-shrink-0 h-5 w-5 text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
              </svg>
              <span class="ml-4 text-sm font-medium text-gray-500 hover:text-gray-700" aria-current="page">True</span>
            </div>
          </li>
        </ol>
      </nav>
    </div>
  }
}
