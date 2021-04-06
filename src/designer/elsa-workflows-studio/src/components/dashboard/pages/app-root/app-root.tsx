import {Component, h, Prop} from '@stencil/core';
import Tunnel, {DashboardState} from '../../../../data/dashboard';

@Component({
  tag: 'app-root',
  styleUrl: 'app-root.css',
  shadow: false,
})
export class AppRoot {

  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;

  render() {

    const serverUrl = this.serverUrl;

    // TODO: Tunneling doesn't appear to be working in combination with the router.
    // const tunnelState: DashboardState = {
    //   serverUrl: serverUrl,
    // };

    return (
      <div class="h-screen bg-gray-100">
        <nav class="bg-gray-800">
          <div class="px-4 sm:px-6 lg:px-8">
            <div class="flex items-center justify-between h-16">
              <div class="flex items-center">
                <div class="flex-shrink-0">
                  <img class="h-8 w-8" src="/build/assets/images/logo.png" alt="Workflow"/>
                </div>
                <div class="hidden md:block">
                  <div class="ml-10 flex items-baseline space-x-4">
                    <stencil-route-link url="/workflow-definitions" anchorClass="text-gray-300 hover:bg-gray-700 hover:text-white px-3 py-2 rounded-md text-sm font-medium" activeClass="text-white bg-gray-900">Workflow Definitions
                    </stencil-route-link>
                    <stencil-route-link url="/workflow-instances" anchorClass="text-gray-300 hover:bg-gray-700 hover:text-white px-3 py-2 rounded-md text-sm font-medium" activeClass="text-white bg-gray-900">Workflow Instances
                    </stencil-route-link>
                    <stencil-route-link url="/workflow-registry" anchorClass="text-gray-300 hover:bg-gray-700 hover:text-white px-3 py-2 rounded-md text-sm font-medium" activeClass="text-white bg-gray-900">Workflow Registry
                    </stencil-route-link>
                    <stencil-route-link url="/custom-activities" class="text-gray-300 hover:bg-gray-700 hover:text-white px-3 py-2 rounded-md text-sm font-medium">Custom Activities</stencil-route-link>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </nav>

        <main>
          {/*<Tunnel.Provider state={tunnelState}>*/}
          <stencil-router>
            <stencil-route-switch scrollTopOffset={0}>
              <stencil-route url="/" routeRender={props => <stencil-route-redirect url="/workflow-registry"/>} exact={true}/>
              <stencil-route url="/workflow-registry" component="app-workflow-registry"/>
              <stencil-route url="/workflow-definitions" component="app-workflow-definitions-list" componentProps={{'serverUrl': serverUrl}} exact={true}/>
              <stencil-route url="/workflow-definitions/:id" component="app-workflow-definitions-edit" componentProps={{'serverUrl': serverUrl}} />
            </stencil-route-switch>
          </stencil-router>
          {/*</Tunnel.Provider>*/}
        </main>
      </div>
    );
  }
}
