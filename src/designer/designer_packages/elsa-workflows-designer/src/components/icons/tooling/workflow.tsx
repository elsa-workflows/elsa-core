import {FunctionalComponent, h} from "@stencil/core";

export const WorkflowIcon: FunctionalComponent = () =>
  <svg class="tw-mr-3 tw-h-5 tw-w-5 tw-text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <rect x="4" y="4" width="6" height="6" rx="1"/>
    <rect x="14" y="4" width="6" height="6" rx="1"/>
    <rect x="4" y="14" width="6" height="6" rx="1"/>
    <rect x="14" y="14" width="6" height="6" rx="1"/>
  </svg>;
