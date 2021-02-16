import { h } from "@stencil/core";

export const ActivityIcon = props => (
  <svg class={props.className} fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path stroke="none" d="M0 0h24v24H0z"/> <polyline points="21 12 17 12 14 20 10 4 7 12 3 12"/>
  </svg>
);
