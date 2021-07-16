import {Component, h, Prop, getAssetPath} from '@stencil/core';
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {i18n} from "i18next";
import {GetIntlMessage} from "../../../i18n/intl-message";

@Component({
  tag: 'elsa-studio-dashboard',
  shadow: false,
  assetsDirs: ['assets']
})
export class ElsaStudioDashboard {

  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path', reflect: true}) monacoLibPath: string;
  @Prop({attribute: 'culture', reflect: true}) culture: string;
  private i18next: i18n;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  render() {

    const serverUrl = this.serverUrl;
    const culture = this.culture;
    const monacoLibPath = this.monacoLibPath;
    const logoPath = getAssetPath('./assets/logo.png');

    // TODO: Tunneling doesn't appear to be working in combination with the router.
    // const tunnelState: DashboardState = {
    //   serverUrl: serverUrl,
    // };
    
    const IntlMessage = GetIntlMessage(this.i18next);

    return (
      <div class="elsa-h-screen elsa-bg-gray-100">
        <nav class="elsa-bg-gray-800">
          <div class="elsa-px-4 sm:elsa-px-6 lg:elsa-px-8">
            <div class="elsa-flex elsa-items-center elsa-justify-between elsa-h-16">
              <div class="elsa-flex elsa-items-center">
                <div class="elsa-flex-shrink-0">
                  <stencil-route-link url="/"><img class="elsa-h-8 elsa-w-8" src={logoPath} alt="Workflow"/></stencil-route-link>
                </div>
                <div class="hidden md:elsa-block">
                  <div class="elsa-ml-10 elsa-flex elsa-items-baseline elsa-space-x-4">
                    <stencil-route-link url="/workflow-definitions" anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">
                      <IntlMessage label="WorkflowDefinitions"/>
                    </stencil-route-link>
                    <stencil-route-link url="/workflow-instances" anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">
                      <IntlMessage label="WorkflowInstances"/>
                    </stencil-route-link>
                    <stencil-route-link url="/workflow-registry" anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">
                      <IntlMessage label="WorkflowRegistry"/>
                    </stencil-route-link>

                    {/*<stencil-route-link url="/custom-activities" class="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium">Custom Activities</stencil-route-link>*/}
                    <stencil-route-link url="/webhook-definitions" anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">
                      <IntlMessage label="WebhookDefinitions"/>
                    </stencil-route-link>
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
              <stencil-route url="/" component="elsa-studio-home" exact={true} componentProps={{culture: culture}}/>
              <stencil-route url="/workflow-registry" component="elsa-studio-workflow-registry" componentProps={{'serverUrl': serverUrl, culture: culture}} exact={true}/>
              <stencil-route url="/workflow-registry/:id" component="elsa-studio-workflow-blueprint-view" componentProps={{'serverUrl': serverUrl, culture: culture}}/>
              <stencil-route url="/workflow-definitions" component="elsa-studio-workflow-definitions-list" componentProps={{'serverUrl': serverUrl, culture: culture}} exact={true}/>
              <stencil-route url="/workflow-definitions/:id" component="elsa-studio-workflow-definitions-edit" componentProps={{'serverUrl': serverUrl, culture: culture, 'monacoLibPath': monacoLibPath}}/>
              <stencil-route url="/workflow-instances" component="elsa-studio-workflow-instances-list" componentProps={{'serverUrl': serverUrl, culture: culture}} exact={true}/>
              <stencil-route url="/workflow-instances/:id" component="elsa-studio-workflow-instances-view" componentProps={{'serverUrl': serverUrl, culture: culture}}/>
              <stencil-route url="/webhook-definitions" component="elsa-studio-webhook-definitions-list" componentProps={{'serverUrl': serverUrl, culture: culture}} exact={true}/>
              <stencil-route url="/webhook-definitions/:id" component="elsa-studio-webhook-definitions-edit" componentProps={{'serverUrl': serverUrl, culture: culture}}/>
            </stencil-route-switch>
          </stencil-router>
          {/*</Tunnel.Provider>*/}
        </main>
        <elsa-external-events serverUrl={serverUrl}/>
      </div>
    );
  }
}
