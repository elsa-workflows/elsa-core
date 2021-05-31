import {h} from "@stencil/core";

export const SwitchIcon = props =>
  (
    `<span class="${`elsa-rounded-lg elsa-inline-flex elsa-p-3 elsa-bg-green-50 elsa-text-green-700 elsa-ring-4 elsa-ring-white`}">
      <svg class="elsa-h-6 elsa-w-6" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <polyline points="15 4 19 4 19 8"/>
        <line x1="14.75" y1="9.25" x2="19" y2="4"/>
        <line x1="5" y1="19" x2="9" y2="15"/>
        <polyline points="15 19 19 19 19 15"/>
        <line x1="5" y1="5" x2="19" y2="19"/>
      </svg>
    </span>`
  );
