import {h} from "@stencil/core";

export const RunWorkflowIcon = props =>
  (
    `<span class="${`elsa-rounded-lg elsa-inline-flex elsa-p-3 elsa-bg-sky-50 elsa-text-sky-700 elsa-ring-4 elsa-ring-white`}">
      <svg class="elsa-h-6 elsa-w-6" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <rect x="3" y="3" width="6" height="6" rx="1"/>
        <rect x="15" y="15" width="6" height="6" rx="1"/>
        <path d="M21 11v-3a2 2 0 0 0 -2 -2h-6l3 3m0 -6l-3 3"/>
        <path d="M3 13v3a2 2 0 0 0 2 2h6l-3 -3m0 6l3 -3"/>
      </svg>
    </span>`
  );
