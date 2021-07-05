import {Component, h, Prop, getAssetPath} from '@stencil/core';

@Component({
  tag: 'elsa-studio-root',
  shadow: false,
  assetsDirs: ['assets']
})
export class ElsaStudioRoot {

  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path', reflect: true}) monacoLibPath: string;

  render() {

    const serverUrl = this.serverUrl;
    const monacoLibPath = this.monacoLibPath;
    const logoPath = getAssetPath('./assets/logo.png');

    // TODO: Tunneling doesn't appear to be working in combination with the router.
    // const tunnelState: DashboardState = {
    //   serverUrl: serverUrl,
    // };

    return (
      <div class="elsa-h-screen elsa-bg-gray-100">
        <nav class="elsa-bg-gray-800">
          <div class="elsa-px-4 sm:elsa-px-6 lg:elsa-px-8">
            <div class="elsa-flex elsa-items-center elsa-justify-between elsa-h-16">
              <div class="elsa-flex elsa-items-center">
                <div class="elsa-flex-shrink-0">
                  <img class="elsa-h-8 elsa-w-8" src={logoPath} alt="Workflow"/>
                </div>
                <div class="hidden md:elsa-block">
                  <div class="elsa-ml-10 elsa-flex elsa-items-baseline elsa-space-x-4">
                    <stencil-route-link url="/workflow-definitions" anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">Workflow Definitions
                    </stencil-route-link>
                    <stencil-route-link url="/workflow-instances" anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">Workflow Instances
                    </stencil-route-link>
                    <stencil-route-link url="/workflow-registry" anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">Workflow Registry
                    </stencil-route-link>

                    {/*<stencil-route-link url="/custom-activities" class="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium">Custom Activities</stencil-route-link>*/}
                    <stencil-route-link url="/webhook-definitions" anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">Webhook Definitions
                    </stencil-route-link>
                    {/*<stencil-route-link url="/custom-activities" class="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium">Custom Activities</stencil-route-link>*/}
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
              <stencil-route url="/" component="elsa-studio-home" exact={true}/>
              <stencil-route url="/workflow-registry" component="elsa-studio-workflow-registry" componentProps={{'serverUrl': serverUrl}} exact={true}/>
              <stencil-route url="/workflow-registry/:id" component="elsa-studio-workflow-blueprint-view" componentProps={{'serverUrl': serverUrl}}/>
              <stencil-route url="/workflow-definitions" component="elsa-studio-workflow-definitions-list" componentProps={{'serverUrl': serverUrl}} exact={true}/>
              <stencil-route url="/workflow-definitions/:id" component="elsa-studio-workflow-definitions-edit" componentProps={{'serverUrl': serverUrl, 'monacoLibPath': monacoLibPath}}/>
              <stencil-route url="/workflow-instances" component="elsa-studio-workflow-instances-list" componentProps={{'serverUrl': serverUrl}} exact={true}/>
              <stencil-route url="/workflow-instances/:id" component="elsa-studio-workflow-instances-view" componentProps={{'serverUrl': serverUrl}}/>
              <stencil-route url="/webhook-definitions" component="elsa-studio-webhook-definitions-list" componentProps={{'serverUrl': serverUrl}} exact={true}/>
              <stencil-route url="/webhook-definitions/:id" component="elsa-studio-webhook-definitions-edit" componentProps={{'serverUrl': serverUrl}}/>
            </stencil-route-switch>
          </stencil-router>
          {/*</Tunnel.Provider>*/}
        </main>
        <elsa-external-events/>
      </div>
    );
  }
}
