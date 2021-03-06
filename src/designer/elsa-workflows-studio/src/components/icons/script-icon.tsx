import {h} from "@stencil/core";

export const ScriptIcon = props =>
  (
    <span class={`rounded-lg inline-flex p-3 bg-light-blue-50 text-light-blue-700 ring-4 ring-white`}>
      <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <polyline points="7 8 3 12 7 16"/>
        <polyline points="17 8 21 12 17 16"/>
        <line x1="14" y1="4" x2="10" y2="20"/>
      </svg>
    </span>
  );
