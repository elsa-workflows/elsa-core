import {Component, h} from "@stencil/core";

@Component({
  tag: 'elsa-blank',
  shadow: false,
})
export class Blank{
  render(){
    return <div class="bg-gray-800 overflow-hidden h-screen"></div>;
  }
}
