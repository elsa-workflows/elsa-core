import {h} from "@stencil/core";

export const LoopIcon = props =>
  (
    `<span class="rounded-lg inline-flex p-3 bg-green-50 text-green-700 ring-4 ring-white">
      <svg class="h-6 w-6" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <path d="M4 12v-3a3 3 0 0 1 3 -3h13m-3 -3l3 3l-3 3"/>
        <path d="M20 12v3a3 3 0 0 1 -3 3h-13m3 3l-3-3l3-3"/>
        <path d="M11 11l1 -1v4"/>
      </svg>
    </span>`
  );
