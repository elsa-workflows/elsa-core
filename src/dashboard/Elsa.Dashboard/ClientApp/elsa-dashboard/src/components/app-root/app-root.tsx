import { Component, h } from '@stencil/core';

@Component({
  tag: 'app-root',
  shadow: false
})
export class AppRoot {

  render() {
    return (

      <main>
        <stencil-router>
          <stencil-route-switch scrollTopOffset={ 0 }>
            <stencil-route url='/elsa-dashboard' component='app-home' exact={ true } />
            <stencil-route url='/elsa-dashboard/workflow-definitions' component='app-workflow-definitions' exact={ true } />
            <stencil-route url='/elsa-dashboard/workflow-definitions/:id' component='app-workflow-editor' />
          </stencil-route-switch>
        </stencil-router>
      </main>
    );
  }
}
