import {FunctionalComponent, h} from "@stencil/core";

export const WorkflowIcon: FunctionalComponent = () =>
  <svg class="mr-3 h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <rect x="4" y="4" width="6" height="6" rx="1"/>
    <rect x="14" y="4" width="6" height="6" rx="1"/>
    <rect x="4" y="14" width="6" height="6" rx="1"/>
    <rect x="14" y="14" width="6" height="6" rx="1"/>
  </svg>;
