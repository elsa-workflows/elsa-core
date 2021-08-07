import {h} from "@stencil/core";

export const ScriptIcon = props =>
  (
    `<span class="${`elsa-rounded-lg elsa-inline-flex elsa-p-3 elsa-bg-sky-50 elsa-text-sky-700 elsa-ring-4 elsa-ring-white`}">
      <svg class="elsa-h-6 elsa-w-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <path d="M15 21h-9a3 3 0 0 1 -3 -3v-1h10v2a2 2 0 0 0 4 0v-14a2 2 0 1 1 2 2h-2m2 -4h-11a3 3 0 0 0 -3 3v11"/>
        <line x1="9" y1="7" x2="13" y2="7"/>
        <line x1="9" y1="11" x2="13" y2="11"/>
      </svg>
    </span>`
  );
