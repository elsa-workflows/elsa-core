import {Component, h} from "@stencil/core";

@Component({
  tag: 'elsa-blank',
  shadow: false,
})
export class Blank{
  render(){
    return <div class="tw-bg-gray-800 tw-overflow-hidden tw-h-screen"></div>;
  }
}
