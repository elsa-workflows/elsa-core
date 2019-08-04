import { Component, h } from '@stencil/core';

@Component({
  tag: 'app-home',
  shadow: false
})
export class AppHome {

  render() {

    return (
      <div class="content-wrapper">
        <div class="content">
          Home
        </div>
      </div>
    );
  }
}
