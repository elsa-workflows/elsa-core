import {h} from "@stencil/core";

export const BreakIcon = props =>
  (
    `<span class="rounded-lg inline-flex p-3 bg-green-50 text-green-700 ring-4 ring-white">
      <svg class="h-6 w-6" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <path d="M14 8v-2a2 2 0 0 0 -2 -2h-7a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h7a2 2 0 0 0 2 -2v-2"/>
        <path d="M7 12h14l-3 -3m0 6l3 -3"/>
      </svg>
    </span>`
  );
