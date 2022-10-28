import {Component, getAssetPath, h, Prop} from '@stencil/core';
import 'i18next-wc';
import {GetIntlMessage, IntlMessage} from "../../../i18n/intl-message";
import {loadTranslations} from "../../../i18n/i18n-loader";
import {resources} from "./localizations";
import {i18n} from "i18next";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-studio-home',
  shadow: false,
  assetsDirs: ['assets']
})
export class ElsaStudioHome {

  @Prop() culture: string;
  @Prop() serverVersion: string;
  private i18next: i18n;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  render() {
    const visualPath = getAssetPath('./assets/undraw_breaking_barriers_vnf3.svg');
    const IntlMessage = GetIntlMessage(this.i18next);
    const serverVersion = this.serverVersion;

    return (
      <div class="elsa-home-wrapper elsa-relative elsa-bg-gray-800 elsa-overflow-hidden elsa-h-screen">
        <main class="elsa-mt-16 sm:elsa-mt-24">
          <div class="elsa-mx-auto elsa-max-w-7xl">
            <div class="lg:elsa-grid lg:elsa-grid-cols-12 lg:elsa-gap-8">
              <div class="elsa-px-4 sm:elsa-px-6 sm:elsa-text-center md:elsa-max-w-2xl md:elsa-mx-auto lg:elsa-col-span-6 lg:elsa-text-left lg:flex lg:elsa-items-center">
                <div class="elsa-home-caption-wrapper">
                  <h1 class="elsa-mt-4 elsa-text-4xl elsa-tracking-tight elsa-font-extrabold elsa-text-white sm:elsa-mt-5 sm:elsa-leading-none lg:elsa-mt-6 lg:elsa-text-5xl xl:elsa-text-6xl">
                    <span class="md:elsa-block"><IntlMessage label="Welcome" dangerous title={`<span class='elsa-text-teal-400 md:elsa-block'>Elsa Workflows</span> <span>${serverVersion}</span>`}/></span>
                  </h1>
                  <p class="tagline elsa-mt-3 elsa-text-base elsa-text-gray-300 sm:elsa-mt-5 sm:elsa-text-xl lg:elsa-text-lg xl:elsa-text-xl">
                    <IntlMessage label="Tagline"/>
                  </p>
                </div>
              </div>
              <div class="elsa-mt-16 sm:elsa-mt-24 lg:elsa-mt-0 lg:elsa-col-span-6">
                <div class="sm:elsa-max-w-md sm:elsa-w-full sm:elsa-mx-auto sm:elsa-rounded-lg sm:elsa-overflow-hidden">
                  <div class="elsa-px-4 elsa-py-8 sm:elsa-px-10">
                    <img class="elsa-home-visual" src={visualPath} alt="" width={400}/>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </main>
      </div>
    );
  }
}
Tunnel.injectProps(ElsaStudioHome, ['culture', 'serverVersion']);
