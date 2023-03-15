import {FunctionalComponent, h} from "@stencil/core";

export const WorkflowStatusIcon: FunctionalComponent = () =>
  <svg class="mr-3 h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <circle cx="12" cy="12" r="10"/>
    <polygon points="10 8 16 12 10 16 10 8"/>
  </svg>;
