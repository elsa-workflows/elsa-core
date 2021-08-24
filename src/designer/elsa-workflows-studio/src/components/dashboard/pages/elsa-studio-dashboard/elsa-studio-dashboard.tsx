import {Component, h, Prop, getAssetPath} from '@stencil/core';
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {i18n} from "i18next";
import {GetIntlMessage} from "../../../i18n/intl-message";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-studio-dashboard',
  shadow: false,
  assetsDirs: ['assets']
})
export class ElsaStudioDashboard {

  @Prop({attribute: 'culture', reflect: true}) culture: string;
  @Prop({attribute: 'base-path', reflect: true}) basePath: string = '';
  private i18next: i18n;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  render() {

    const logoPath = getAssetPath('./assets/logo.png');
    const basePath = this.basePath || '';
    const IntlMessage = GetIntlMessage(this.i18next);

    return (
      <div class="elsa-h-screen elsa-bg-gray-100">
        <nav class="elsa-bg-gray-800">
          <div class="elsa-px-4 sm:elsa-px-6 lg:elsa-px-8">
            <div class="elsa-flex elsa-items-center elsa-justify-between elsa-h-16">
              <div class="elsa-flex elsa-items-center">
                <div class="elsa-flex-shrink-0">
                  <stencil-route-link url={`${basePath}/`}><img class="elsa-h-8 elsa-w-8" src={logoPath} alt="Workflow"/></stencil-route-link>
                </div>
                <div class="hidden md:elsa-block">
                  <div class="elsa-ml-10 elsa-flex elsa-items-baseline elsa-space-x-4">
                    <stencil-route-link url={`${basePath}/workflow-definitions`} anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">
                      <IntlMessage label="WorkflowDefinitions"/>
                    </stencil-route-link>
                    <stencil-route-link url={`${basePath}/workflow-instances`} anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">
                      <IntlMessage label="WorkflowInstances"/>
                    </stencil-route-link>
                    <stencil-route-link url={`${basePath}/workflow-registry`} anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
                                        activeClass="elsa-text-white elsa-bg-gray-900">
                      <IntlMessage label="WorkflowRegistry"/>
                    </stencil-route-link>

                    {/*<stencil-route-link url="/custom-activities" class="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium">Custom Activities</stencil-route-link>*/}
                    <stencil-route-link url={`${basePath}/webhook-definitions`} anchorClass="elsa-text-gray-300 hover:elsa-bg-gray-700 hover:elsa-text-white elsa-px-3 elsa-py-2 elsa-rounded-md elsa-text-sm elsa-font-medium"
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
          <stencil-router>
            <stencil-route-switch scrollTopOffset={0}>
              <stencil-route url={`${basePath}/`} component="elsa-studio-home" exact={true}/>
              <stencil-route url={`${basePath}/workflow-registry`} component="elsa-studio-workflow-registry" exact={true}/>
              <stencil-route url={`${basePath}/workflow-registry/:id`} component="elsa-studio-workflow-blueprint-view"/>
              <stencil-route url={`${basePath}/workflow-definitions`} component="elsa-studio-workflow-definitions-list" exact={true}/>
              <stencil-route url={`${basePath}/workflow-definitions/:id`} component="elsa-studio-workflow-definitions-edit"/>
              <stencil-route url={`${basePath}/workflow-instances`} component="elsa-studio-workflow-instances-list" exact={true}/>
              <stencil-route url={`${basePath}/workflow-instances/:id`} component="elsa-studio-workflow-instances-view"/>
              <stencil-route url={`${basePath}/webhook-definitions`} component="elsa-studio-webhook-definitions-list" exact={true}/>
              <stencil-route url={`${basePath}/webhook-definitions/:id`} component="elsa-studio-webhook-definitions-edit"/>
            </stencil-route-switch>
          </stencil-router>
        </main>
      </div>
    );
  }
}
Tunnel.injectProps(ElsaStudioDashboard, ['culture', 'basePath']);
