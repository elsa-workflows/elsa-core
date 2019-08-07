import { Component, h, Prop } from '@stencil/core';
import { RouterHistory } from "@stencil/router";

@Component({
  tag: 'app-home',
  shadow: false
})
export class AppHome {
  @Prop() history: RouterHistory;

  componentDidLoad(){
    this.history.replace('/elsa-dashboard/workflow-definitions');
  }

  render() {

    return (
      <div/>
    );
  }
}
